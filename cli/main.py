"""gtool – Game Translation Pipeline CLI.

Usage examples::

    gtool extract game.assets --adapter unity --output text_dump.json
    gtool translate text_dump.json --provider deepl --api-key KEY --glossary terms.json
    gtool validate text_dump.json --report warnings.txt
    gtool rebuild game.assets text_dump.json --output translated/
    gtool patch game.assets translated_game.assets --output game_english.xdelta
"""

from __future__ import annotations

import json
import logging
import os
import sys

import click

# ---------------------------------------------------------------------------
# Logging setup
# ---------------------------------------------------------------------------
logging.basicConfig(
    format="%(levelname)s: %(message)s",
    level=logging.INFO,
    stream=sys.stderr,
)
logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# CLI group
# ---------------------------------------------------------------------------

@click.group()
@click.version_option(version="1.0.0", prog_name="gtool")
def cli() -> None:
    """gtool – modular game translation pipeline."""


# ---------------------------------------------------------------------------
# extract
# ---------------------------------------------------------------------------

@cli.command()
@click.argument("input_file", type=click.Path(exists=True, dir_okay=False))
@click.option(
    "--adapter",
    default="unity",
    show_default=True,
    help="Format adapter to use (currently: unity).",
)
@click.option(
    "--output",
    "-o",
    default="text_dump.json",
    show_default=True,
    help="Output JSON file path.",
)
def extract(input_file: str, adapter: str, output: str) -> None:
    """Extract translatable text from INPUT_FILE."""
    from adapters.unity import UnityAdapter
    from adapters.base import BaseAdapter

    adapters: dict[str, BaseAdapter] = {
        "unity": UnityAdapter(),
    }

    if adapter not in adapters:
        raise click.BadParameter(
            f"Unknown adapter {adapter!r}. Available: {', '.join(adapters)}",
            param_hint="--adapter",
        )

    selected = adapters[adapter]
    if not selected.detect(input_file):
        logger.warning(
            "Adapter %r may not support this file type. Proceeding anyway.", adapter
        )

    with click.progressbar(length=1, label="Extracting") as bar:
        entries = selected.extract(input_file)
        bar.update(1)

    data = [e.to_dict() for e in entries]
    os.makedirs(os.path.dirname(os.path.abspath(output)), exist_ok=True)
    with open(output, "w", encoding="utf-8") as fh:
        json.dump(data, fh, ensure_ascii=False, indent=2)

    click.echo(f"Extracted {len(entries)} entries → {output}")


# ---------------------------------------------------------------------------
# translate
# ---------------------------------------------------------------------------

@cli.command()
@click.argument("input_json", type=click.Path(exists=True, dir_okay=False))
@click.option("--provider", default="deepl", show_default=True)
@click.option("--api-key", envvar="GTOOL_API_KEY", default="", help="API key.")
@click.option("--glossary", default="glossary.json", show_default=True)
@click.option("--source-lang", default="JA", show_default=True)
@click.option("--target-lang", default="EN-US", show_default=True)
@click.option(
    "--output",
    "-o",
    default=None,
    help="Output JSON path (default: overwrites input_json).",
)
def translate(
    input_json: str,
    provider: str,
    api_key: str,
    glossary: str,
    source_lang: str,
    target_lang: str,
    output: str | None,
) -> None:
    """Translate entries in INPUT_JSON and write results back."""
    from adapters.base import TextEntry
    from core.translator import translate_batch

    with open(input_json, encoding="utf-8") as fh:
        data = json.load(fh)

    entries = [TextEntry.from_dict(d) for d in data]

    click.echo(f"Translating {len(entries)} entries via {provider}…")

    translated = translate_batch(
        entries,
        provider=provider,
        api_key=api_key,
        glossary_path=glossary,
        source_lang=source_lang,
        target_lang=target_lang,
    )

    out_path = output or input_json
    with open(out_path, "w", encoding="utf-8") as fh:
        json.dump([e.to_dict() for e in translated], fh, ensure_ascii=False, indent=2)

    click.echo(f"Translations saved → {out_path}")


# ---------------------------------------------------------------------------
# validate
# ---------------------------------------------------------------------------

@cli.command()
@click.argument("input_json", type=click.Path(exists=True, dir_okay=False))
@click.option(
    "--report",
    "-r",
    default=None,
    help="Write a warnings report to this file.",
)
def validate(input_json: str, report: str | None) -> None:
    """Check INPUT_JSON entries for overflow / control-code issues."""
    from adapters.base import TextEntry
    from utils.validation import check_overflow

    with open(input_json, encoding="utf-8") as fh:
        data = json.load(fh)

    entries = [TextEntry.from_dict(d) for d in data]
    warnings: list[str] = []

    for entry in entries:
        result = check_overflow(entry)
        if result["overflow"]:
            msg = (
                f"[{result['risk_level'].upper()}] {entry.id}: "
                f"orig={result['original_bytes']}B "
                f"trans={result['translated_bytes']}B "
                f"lines {result['original_lines']}→{result['translated_lines']} "
                f"missing_codes={result['missing_codes']}"
            )
            warnings.append(msg)
            click.echo(msg)

    click.echo(f"\n{len(warnings)} issue(s) found in {len(entries)} entries.")

    if report:
        with open(report, "w", encoding="utf-8") as fh:
            fh.write("\n".join(warnings))
        click.echo(f"Report saved → {report}")


# ---------------------------------------------------------------------------
# rebuild
# ---------------------------------------------------------------------------

@cli.command()
@click.argument("original_file", type=click.Path(exists=True, dir_okay=False))
@click.argument("translations_json", type=click.Path(exists=True, dir_okay=False))
@click.option(
    "--adapter",
    default="unity",
    show_default=True,
    help="Format adapter to use.",
)
@click.option(
    "--output",
    "-o",
    default="translated_output",
    show_default=True,
    help="Output directory.",
)
def rebuild(
    original_file: str,
    translations_json: str,
    adapter: str,
    output: str,
) -> None:
    """Rebuild ORIGINAL_FILE with translations from TRANSLATIONS_JSON."""
    from adapters.base import TextEntry
    from adapters.unity import UnityAdapter

    adapters = {"unity": UnityAdapter()}
    if adapter not in adapters:
        raise click.BadParameter(f"Unknown adapter {adapter!r}.", param_hint="--adapter")

    with open(translations_json, encoding="utf-8") as fh:
        data = json.load(fh)
    entries = [TextEntry.from_dict(d) for d in data]

    os.makedirs(output, exist_ok=True)
    out_path = os.path.join(output, os.path.basename(original_file))
    ok = adapters[adapter].rebuild(original_file, entries, out_path)

    if ok:
        click.echo(f"Rebuilt file written → {out_path}")
    else:
        click.echo("Rebuild completed with some errors (see log).", err=True)
        sys.exit(1)


# ---------------------------------------------------------------------------
# patch
# ---------------------------------------------------------------------------

@cli.command()
@click.argument("original_file", type=click.Path(exists=True, dir_okay=False))
@click.argument("modified_file", type=click.Path(exists=True, dir_okay=False))
@click.option(
    "--output",
    "-o",
    default="game_patch.xdelta",
    show_default=True,
    help="Output patch file path.",
)
@click.option(
    "--format",
    "fmt",
    default="xdelta",
    show_default=True,
    type=click.Choice(["xdelta", "ips", "bps"], case_sensitive=False),
)
def patch(original_file: str, modified_file: str, output: str, fmt: str) -> None:
    """Generate a binary patch from ORIGINAL_FILE to MODIFIED_FILE."""
    from core.patcher import generate_patch

    ok = generate_patch(original_file, modified_file, output, format=fmt)
    if ok:
        click.echo(f"Patch created → {output}")
    else:
        click.echo("Patch generation failed (see log).", err=True)
        sys.exit(1)


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> None:
    """Entry point used by the ``gtool`` console script."""
    cli()


if __name__ == "__main__":
    main()
