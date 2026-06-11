const MARKER_REGEX = /\{\{([^}]+)\}\}/g;

/** Extrae marcadores {{...}} del HTML (misma regex que TemplateResolver backend). */
export function detectTemplateMarkers(html: string): string[] {
  if (!html.trim()) return [];

  const found = new Set<string>();
  for (const match of html.matchAll(MARKER_REGEX)) {
    const marker = match[1]?.trim();
    if (marker) found.add(marker);
  }

  return [...found].sort((a, b) => a.localeCompare(b));
}
