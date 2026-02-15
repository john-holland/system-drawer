#!/usr/bin/env python3
"""
Region massager: secure overwrite then delete.

Overwrites a file's on-disk content with random data, then deletes the file,
so undelete or file-recovery tools cannot restore the original (e.g. to prevent
malware from respawning via undelete). Run from a clean environment (or after
closing the process that uses the file) if the file is locked.
"""
import argparse
import os
import sys


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Overwrite a file with random data then delete it (secure delete)."
    )
    parser.add_argument(
        "path",
        nargs="?",
        default=None,
        help="Path to the file to overwrite and delete (or use --path).",
    )
    parser.add_argument(
        "--path",
        dest="path_option",
        type=str,
        default=None,
        help="Path to the file to overwrite and delete (alternative to positional path).",
    )
    parser.add_argument(
        "--passes",
        type=int,
        default=1,
        metavar="N",
        help="Number of overwrite passes (default: 1).",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Skip confirmation prompt.",
    )
    args = parser.parse_args()
    path_arg = args.path_option or args.path
    if not path_arg:
        parser.error("Missing file path. Provide it as a positional argument or use --path.")
    path = os.path.abspath(os.path.normpath(path_arg))

    if not os.path.exists(path):
        print(f"Error: path does not exist: {path}", file=sys.stderr)
        return 1
    if os.path.isdir(path):
        print(f"Error: path is a directory (single file only): {path}", file=sys.stderr)
        return 1

    if not args.force:
        try:
            reply = input("Overwrite and delete this file? [y/N] ").strip().lower()
        except (EOFError, KeyboardInterrupt):
            print("Aborted.", file=sys.stderr)
            return 1
        if reply not in ("y", "yes"):
            print("Aborted.", file=sys.stderr)
            return 1

    try:
        with open(path, "r+b") as f:
            size = f.seek(0, 2)
            if size == 0:
                f.close()
                os.remove(path)
                print("File was empty; deleted.")
                return 0
            for pass_num in range(1, args.passes + 1):
                f.seek(0)
                f.write(os.urandom(size))
                f.flush()
                if hasattr(os, "fsync"):
                    os.fsync(f.fileno())
                if args.passes > 1:
                    print(f"Pass {pass_num}/{args.passes} done.")
    except PermissionError as e:
        print(
            f"Error: permission denied opening or writing the file: {e}",
            file=sys.stderr,
        )
        print(
            "If the file is in use (e.g. by malware), close that process or run this "
            "from a clean environment (e.g. bootable media). You may need elevated rights.",
            file=sys.stderr,
        )
        return 1
    except OSError as e:
        print(f"Error: could not open or write the file: {e}", file=sys.stderr)
        print(
            "If the file is in use, close the process that has it open or run from a "
            "clean environment (e.g. bootable media).",
            file=sys.stderr,
        )
        return 1

    try:
        os.remove(path)
    except OSError as e:
        print(f"Error: overwrite succeeded but delete failed: {e}", file=sys.stderr)
        return 1

    print("File overwritten and deleted.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
