"""Shared ClauseRef model and binding_kind taxonomy."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, Optional

BINDING_KIND_PROPERTY = "property"
BINDING_KIND_LEMMA = "lemma"
BINDING_KIND_LOCALIZATION = "localization"
BINDING_KIND_PROMPT = "prompt_placeholder"

BINDING_KINDS = frozenset(
    {BINDING_KIND_PROPERTY, BINDING_KIND_LEMMA, BINDING_KIND_LOCALIZATION, BINDING_KIND_PROMPT}
)


@dataclass
class ClauseRef:
    farey_left_num: int = 0
    farey_left_den: int = 1
    farey_right_num: int = 1
    farey_right_den: int = 1
    char_start: int = 0
    char_end: int = 0
    selection_text: str = ""
    ast_node_id: Optional[str] = None
    entry_id: Optional[str] = None
    draft_script_id: Optional[str] = None
    episode_script_id: Optional[str] = None

    @classmethod
    def from_body(cls, body: Dict[str, Any]) -> "ClauseRef":
        return cls(
            farey_left_num=int(body.get("fareyLeftNum", 0)),
            farey_left_den=int(body.get("fareyLeftDen", 1)),
            farey_right_num=int(body.get("fareyRightNum", 1)),
            farey_right_den=int(body.get("fareyRightDen", 1)),
            char_start=int(body.get("charStart", 0)),
            char_end=int(body.get("charEnd", 0)),
            selection_text=body.get("selectionText") or "",
            ast_node_id=body.get("astNodeId"),
            entry_id=body.get("entryId"),
            draft_script_id=body.get("draftScriptId"),
            episode_script_id=body.get("episodeScriptId"),
        )

    def to_api(self) -> Dict[str, Any]:
        return {
            "fareyLeftNum": self.farey_left_num,
            "fareyLeftDen": self.farey_left_den,
            "fareyRightNum": self.farey_right_num,
            "fareyRightDen": self.farey_right_den,
            "charStart": self.char_start,
            "charEnd": self.char_end,
            "selectionText": self.selection_text,
            "astNodeId": self.ast_node_id,
            "entryId": self.entry_id,
            "draftScriptId": self.draft_script_id,
            "episodeScriptId": self.episode_script_id,
        }
