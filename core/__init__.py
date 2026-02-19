"""Core pipeline logic for translation, patching, and text graph management."""

from core.translator import translate_entry, translate_batch
from core.patcher import generate_patch
from core.text_graph import TextNode, TextGraph

__all__ = ["translate_entry", "translate_batch", "generate_patch", "TextNode", "TextGraph"]
