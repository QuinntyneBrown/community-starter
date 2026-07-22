#!/usr/bin/env python3
"""Audit the generated detailed-design tree against docs/specs and house rules."""

from __future__ import annotations

import re
import sys
from collections import Counter
from pathlib import Path

import generate_detailed_designs as generator


ROOT = generator.REPO_ROOT
DESIGNS = generator.DESIGNS_ROOT


def fail(errors: list[str], message: str) -> None:
    errors.append(message)


def table_rows(readme: str) -> list[tuple[str, str, str]]:
    matches = re.finditer(
        r"^\| `(L2-[A-Z]+-[0-9]+)` \| `(L1-[A-Z]+-[0-9]+)` \| (.*) \|$",
        readme,
        re.MULTILINE,
    )
    return [(match.group(1), match.group(2), match.group(3)) for match in matches]


def prose_without_requirements(readme: str) -> str:
    before, remainder = readme.split("## Requirements", 1)
    _, diagrams = remainder.split("## Diagrams", 1)
    prose = before + diagrams
    prose = re.sub(r"```.*?```", "", prose, flags=re.DOTALL)
    prose = re.sub(r"`[^`]+`", "", prose)
    prose = re.sub(r"!\[[^]]*]\([^)]+\)", "", prose)
    return prose


def verify() -> list[str]:
    errors: list[str] = []
    subsystems = tuple(
        generator.parse_subsystem(folder)
        for folder in sorted(generator.SPECS_ROOT.iterdir())
        if folder.is_dir()
    )
    expected_requirements = {
        requirement.identifier: requirement
        for subsystem in subsystems
        for feature in subsystem.features
        for requirement in feature.requirements
    }
    expected_readmes: set[Path] = set()
    expected_puml: set[Path] = set()
    expected_png: set[Path] = set()
    seen_rows: Counter[str] = Counter()

    banned = re.compile(
        r"\b(?:we|our|ours|us|you|your|yours|must|very|robust|leverage|seamless|simply|obviously)\b",
        re.IGNORECASE,
    )

    for subsystem in subsystems:
        subsystem_root = DESIGNS / subsystem.folder
        if not subsystem_root.is_dir():
            fail(errors, f"Missing subsystem directory: {subsystem_root.relative_to(ROOT)}")
            continue

        for feature in subsystem.features:
            slug = generator.FEATURE_SLUGS[feature.identifier]
            if not re.fullmatch(r"[a-z]+(?:-[a-z0-9]+)+", slug):
                fail(errors, f"Feature slug is not kebab-case: {slug}")
            feature_root = subsystem_root / slug
            diagrams = feature_root / "diagrams"
            readme_path = feature_root / "README.md"
            expected_readmes.add(readme_path)
            expected_sources = {
                diagrams / "c4-context.puml",
                diagrams / "c4-container.puml",
                diagrams / "c4-component.puml",
                diagrams / "class-structure.puml",
                *{
                    diagrams / f"sequence-{requirement.identifier.lower()}.puml"
                    for requirement in feature.requirements
                },
            }
            expected_puml.update(expected_sources)
            expected_png.update(path.with_suffix(".png") for path in expected_sources)

            if not readme_path.is_file():
                fail(errors, f"Missing README: {readme_path.relative_to(ROOT)}")
                continue
            readme = readme_path.read_text(encoding="utf-8")

            headings = re.findall(r"^## (.+)$", readme, re.MULTILINE)
            if headings != ["Overview", "Description", "Requirements", "Diagrams"]:
                fail(errors, f"Unexpected H2 headings in {readme_path.relative_to(ROOT)}: {headings}")

            rows = table_rows(readme)
            expected_ids = [requirement.identifier for requirement in feature.requirements]
            if [row[0] for row in rows] != expected_ids:
                fail(
                    errors,
                    f"Requirement order/coverage mismatch in {readme_path.relative_to(ROOT)}: "
                    f"expected {expected_ids}, found {[row[0] for row in rows]}",
                )
            for l2_id, parent, text in rows:
                seen_rows[l2_id] += 1
                source = expected_requirements.get(l2_id)
                if source is None:
                    fail(errors, f"Unknown requirement {l2_id} in {readme_path.relative_to(ROOT)}")
                    continue
                if parent != source.parent:
                    fail(errors, f"Wrong parent for {l2_id}: {parent} != {source.parent}")
                actual_text = generator.compact(text.replace("\\|", "|"))
                if actual_text != source.text:
                    fail(errors, f"Requirement text changed for {l2_id}")

            links = re.findall(r"!\[[^]]*]\(([^)]+)\)", readme)
            expected_link_count = 4 + len(feature.requirements)
            if len(links) != expected_link_count:
                fail(
                    errors,
                    f"Image-link count in {readme_path.relative_to(ROOT)} is {len(links)}, "
                    f"expected {expected_link_count}",
                )
            for link in links:
                target = feature_root / link
                if not target.is_file() or target.stat().st_size == 0:
                    fail(errors, f"Broken image link {link} in {readme_path.relative_to(ROOT)}")

            prose = prose_without_requirements(readme)
            style_hits = sorted({match.group(0).lower() for match in banned.finditer(prose)})
            if style_hits:
                fail(errors, f"House-style terms in {readme_path.relative_to(ROOT)}: {style_hits}")
            if "?" in prose:
                fail(errors, f"Rhetorical-question marker in {readme_path.relative_to(ROOT)}")

            for source in expected_sources:
                if not source.is_file():
                    fail(errors, f"Missing PlantUML source: {source.relative_to(ROOT)}")
                    continue
                image = source.with_suffix(".png")
                if not image.is_file() or image.stat().st_size == 0:
                    fail(errors, f"Missing rendered diagram: {image.relative_to(ROOT)}")
                source_text = source.read_text(encoding="utf-8")

                if source.name.startswith("c4-"):
                    if not re.search(r"!include <C4/C4_(?:Context|Container|Component)>", source_text):
                        fail(errors, f"Missing C4 include: {source.relative_to(ROOT)}")
                    if not re.search(r"^Rel(?:_[A-Z]+)?\(", source_text, re.MULTILINE):
                        fail(errors, f"C4 diagram has no Rel macro: {source.relative_to(ROOT)}")
                    if re.search(r"^\s*(?:rectangle|component|node)\b", source_text, re.MULTILINE):
                        fail(errors, f"Raw shape in C4 diagram: {source.relative_to(ROOT)}")
                    if re.search(
                        r"^\s*[A-Za-z_][A-Za-z0-9_]*\s+(?:-->|<--|\.\.>|<\.\.|-+>)",
                        source_text,
                        re.MULTILINE,
                    ):
                        fail(errors, f"Bare relationship in C4 diagram: {source.relative_to(ROOT)}")

                if source.name == "class-structure.puml":
                    if "interface " not in source_text or not re.search(r"(?:-->|\.\.>)", source_text):
                        fail(errors, f"Class structure lacks typed relationships: {source.relative_to(ROOT)}")

                if source.name.startswith("sequence-"):
                    requirement_id = source.stem.removeprefix("sequence-").upper()
                    if source_text.count('box "Frontend — ') != 1:
                        fail(errors, f"Sequence lacks one frontend box: {source.relative_to(ROOT)}")
                    if source_text.count('box "Backend — ') != 3:
                        fail(errors, f"Sequence lacks three backend boxes: {source.relative_to(ROOT)}")
                    if requirement_id not in source_text:
                        fail(errors, f"Sequence lacks requirement trace {requirement_id}: {source.relative_to(ROOT)}")
                    if "alt Decision is accepted" not in source_text or "else Decision is rejected" not in source_text:
                        fail(errors, f"Sequence lacks alternate behavior: {source.relative_to(ROOT)}")

    actual_readmes = set(DESIGNS.rglob("README.md"))
    actual_puml = set(DESIGNS.rglob("*.puml"))
    actual_png = set(DESIGNS.rglob("*.png"))
    for label, expected, actual in (
        ("README", expected_readmes, actual_readmes),
        ("PlantUML", expected_puml, actual_puml),
        ("PNG", expected_png, actual_png),
    ):
        missing = sorted(path.relative_to(ROOT) for path in expected - actual)
        extra = sorted(path.relative_to(ROOT) for path in actual - expected)
        if missing:
            fail(errors, f"Missing {label} files: {missing}")
        if extra:
            fail(errors, f"Unexpected {label} files: {extra}")

    missing_rows = sorted(set(expected_requirements) - seen_rows.keys())
    duplicate_rows = sorted(identifier for identifier, count in seen_rows.items() if count != 1)
    if missing_rows:
        fail(errors, f"L2 requirements absent from design tables: {missing_rows}")
    if duplicate_rows:
        fail(errors, f"L2 requirements repeated across design tables: {duplicate_rows}")

    return errors


def main() -> int:
    errors = verify()
    if errors:
        print(f"Detailed-design audit failed with {len(errors)} error(s):", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print(
        "Detailed-design audit passed: 19 subsystems, 82 features, 260 exact L2 rows, "
        "588 PlantUML sources, 588 rendered PNGs, and all image links resolved."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
