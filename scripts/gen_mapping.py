#!/usr/bin/env python3
# gen_mapping.py - Convert a *.build.appxrecipe into a makeappx mapping file.
# Usage: python gen_mapping.py <path-to.build.appxrecipe> [output-mapping.txt]
# Reads every ItemGroup element that carries an Include attribute and a child
# <PackagePath> and emits a [Files] mapping for makeappx.exe pack.

import os
import sys
import xml.etree.ElementTree as ET


def main():
    if len(sys.argv) < 2:
        print("usage: gen_mapping.py <appxrecipe> [mapping.txt]")
        sys.exit(2)

    recipe = sys.argv[1]
    out = sys.argv[2] if len(sys.argv) > 2 else (os.path.splitext(recipe)[0] + ".mapping.txt")

    tree = ET.parse(recipe)
    root = tree.getroot()

    def find_pkgpath(el):
        # Appxrecipe may carry a default MSBuild namespace; match with wildcard.
        for tag in ("PackagePath", "{*}PackagePath"):
            p = el.find(tag)
            if p is not None and p.text:
                return p.text.strip()
        return None

    entries = []
    for el in root.iter():
        inc = el.get("Include")
        if not inc:
            continue
        pkg = find_pkgpath(el)
        if not pkg:
            continue
        entries.append((inc, pkg))

    # De-duplicate by package path, keep first occurrence.
    # Exclude leftover libVLC assets (reverted to mpv plugin).
    seen = set()
    unique = []
    for src, pkg in entries:
        if pkg in seen:
            continue
        if "libvlc" in pkg.lower() or "libvlc" in src.lower():
            continue
        seen.add(pkg)
        unique.append((src, pkg))

    with open(out, "w", encoding="utf-8") as f:
        f.write("[Files]\n")
        for src, pkg in unique:
            f.write(f'"{src}" "{pkg}"\n')

    print(f"wrote {len(unique)} file entries -> {out}")


if __name__ == "__main__":
    main()
