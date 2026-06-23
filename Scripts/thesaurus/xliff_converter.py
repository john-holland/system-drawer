"""
Two-way XLIFF converter for thesaurus_translations.
Export: DB (thesaurus_entries + thesaurus_translations) -> XLIFF 2.0 XML.
Import: XLIFF XML -> update thesaurus_translations (form = target text).
Unit id = entry_id for stable round-trip.
"""

from __future__ import annotations

import xml.etree.ElementTree as ET
from pathlib import Path
from typing import BinaryIO

from thesaurus.language_resolver import resolve_language_id

XLIFF_NS = "urn:oasis:names:tc:xliff:document:2.0"
XML_NS = "http://www.w3.org/XML/1998/namespace"


def _register_ns():
    ET.register_namespace("", XLIFF_NS)
    ET.register_namespace("x", XLIFF_NS)


def export_to_xliff(
    conn,
    source_lang_code: str,
    target_lang_code: str,
    file_id: str = "thesaurus",
) -> str:
    """
    Export thesaurus_entries (source language) and thesaurus_translations (target language) to XLIFF 2.0 XML.
    Returns XML string. Unit id = entry_id for round-trip.
    """
    source_language_id = resolve_language_id(conn, source_lang_code, create=True)
    if not source_language_id:
        raise ValueError(f"Source language not found: {source_lang_code}")
    target_language_id = resolve_language_id(conn, target_lang_code, create=True)
    if not target_language_id:
        raise ValueError(f"Target language not found: {target_lang_code}")
    cur = conn.execute(
        """SELECT e.id AS entry_id, e.term, e.pos_tag
           FROM thesaurus_entries e
           LEFT JOIN thesaurus_translations t ON t.entry_id = e.id AND t.language_id = ?
           WHERE e.language_id = ?
           ORDER BY e.term, e.pos_tag""",
        (target_language_id, source_language_id),
    )
    rows = cur.fetchall()
    cur = conn.execute(
        "SELECT entry_id, form FROM thesaurus_translations WHERE language_id = ?",
        (target_language_id,),
    )
    translations = {r["entry_id"]: r["form"] for r in cur.fetchall()}
    root = ET.Element(ET.QName(XLIFF_NS, "xliff"))
    root.set("version", "2.0")
    root.set("srcLang", source_lang_code)
    root.set("trgLang", target_lang_code)
    file_el = ET.SubElement(root, ET.QName(XLIFF_NS, "file"))
    file_el.set("id", file_id)
    file_el.set("original", "thesaurus")
    for r in rows:
        entry_id = r["entry_id"]
        unit = ET.SubElement(file_el, ET.QName(XLIFF_NS, "unit"))
        unit.set("id", entry_id)
        seg = ET.SubElement(unit, ET.QName(XLIFF_NS, "segment"))
        src = ET.SubElement(seg, ET.QName(XLIFF_NS, "source"))
        term = (r["term"] or "").strip()
        src.text = term
        if " " in term:
            src.set(f"{{{XML_NS}}}space", "preserve")
        tgt = ET.SubElement(seg, ET.QName(XLIFF_NS, "target"))
        tgt.text = (translations.get(entry_id) or "").strip()
    return ET.tostring(root, encoding="unicode", method="xml")


def import_from_xliff(conn, xliff_content: str | Path | BinaryIO) -> tuple[int, int]:
    """
    Parse XLIFF and update thesaurus_translations for target language.
    xliff_content: XML string, path to file, or file-like object.
    Returns (updated_count, inserted_count).
    """
    import uuid as _uuid
    if isinstance(xliff_content, Path):
        root = ET.parse(xliff_content).getroot()
    elif isinstance(xliff_content, str):
        root = ET.fromstring(xliff_content)
    else:
        root = ET.parse(xliff_content).getroot()
    trg_lang = root.get("trgLang")
    if not trg_lang and root.attrib:
        for k, v in root.attrib.items():
            if "trgLang" in k or k == "trgLang":
                trg_lang = v
                break
    if not trg_lang:
        raise ValueError("XLIFF trgLang not found")
    target_language_id = resolve_language_id(conn, trg_lang.strip(), create=True)
    if not target_language_id:
        raise ValueError(f"Target language not found in DB: {trg_lang}")
    updated = 0
    inserted = 0
    for unit in root.iter():
        if unit.tag is None or "unit" not in unit.tag:
            continue
        uid = unit.get("id")
        if not uid:
            continue
        target_text = None
        for seg in unit:
            if seg.tag is None or "segment" not in seg.tag:
                continue
            for child in seg:
                if child.tag and "target" in child.tag:
                    target_text = (child.text or "").strip()
                    break
            break
        if target_text is None:
            continue
        cur = conn.execute(
            "SELECT id FROM thesaurus_translations WHERE entry_id = ? AND language_id = ?",
            (uid, target_language_id),
        )
        existing = cur.fetchone()
        if existing:
            conn.execute(
                "UPDATE thesaurus_translations SET form = ? WHERE entry_id = ? AND language_id = ?",
                (target_text, uid, target_language_id),
            )
            updated += 1
        else:
            conn.execute(
                "INSERT INTO thesaurus_translations (id, entry_id, language_id, form) VALUES (?, ?, ?, ?)",
                (str(_uuid.uuid4()), uid, target_language_id, target_text),
            )
            inserted += 1
    conn.commit()
    return (updated, inserted)
