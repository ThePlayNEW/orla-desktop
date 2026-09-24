// Renders the brand SVGs in assets/brand into the app icon, tray icons and documentation PNGs.
import { readFileSync, writeFileSync, mkdirSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Resvg } from '@resvg/resvg-js';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const brand = join(root, 'assets', 'brand');
const svg = (name) => readFileSync(join(brand, name), 'utf8');

function render(source, size) {
  const image = new Resvg(source, { fitTo: { mode: 'width', value: size } }).render();
  return { size, png: image.asPng(), rgba: image.pixels };
}

// Small frames are stored as 32-bit DIBs because GDI+ on .NET Framework rejects PNG frames below 256 px.
function dib({ size, rgba }) {
  const maskStride = Math.ceil(size / 32) * 4;
  const buffer = Buffer.alloc(40 + size * size * 4 + maskStride * size);
  buffer.writeUInt32LE(40, 0);
  buffer.writeInt32LE(size, 4);
  buffer.writeInt32LE(size * 2, 8);
  buffer.writeUInt16LE(1, 12);
  buffer.writeUInt16LE(32, 14);
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const from = ((size - 1 - y) * size + x) * 4;
      const to = 40 + (y * size + x) * 4;
      buffer[to] = rgba[from + 2];
      buffer[to + 1] = rgba[from + 1];
      buffer[to + 2] = rgba[from];
      buffer[to + 3] = rgba[from + 3];
    }
  }
  return buffer;
}

function ico(frames) {
  const header = Buffer.alloc(6 + frames.length * 16);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(frames.length, 4);
  let offset = header.length;
  const bodies = frames.map((frame, index) => {
    const body = frame.size >= 256 ? frame.png : dib(frame);
    const entry = 6 + index * 16;
    header[entry] = frame.size >= 256 ? 0 : frame.size;
    header[entry + 1] = frame.size >= 256 ? 0 : frame.size;
    header.writeUInt16LE(1, entry + 4);
    header.writeUInt16LE(32, entry + 6);
    header.writeUInt32LE(body.length, entry + 8);
    header.writeUInt32LE(offset, entry + 12);
    offset += body.length;
    return body;
  });
  return Buffer.concat([header, ...bodies]);
}

const appFrames = [
  render(svg('icon-16.svg'), 16),
  ...[20, 24].map((size) => render(svg('icon-24.svg'), size)),
  ...[32, 40, 48, 64, 256].map((size) => render(svg('icon-48.svg'), size)),
];
writeFileSync(join(root, 'src', 'Orla', 'Orla.ico'), ico(appFrames));

const assets = join(root, 'src', 'Orla', 'Assets');
mkdirSync(assets, { recursive: true });
for (const tone of ['light', 'dark']) {
  const source = svg(`tray-${tone}.svg`);
  writeFileSync(join(assets, `tray-${tone}.ico`), ico([16, 20, 24, 32].map((size) => render(source, size))));
}

writeFileSync(join(root, 'docs', 'icon.png'), render(svg('icon-48.svg'), 128).png);

// Interface glyphs: one stroked path per SVG in assets/glyphs, exposed to XAML as Glyph.<PascalName> geometries.
const glyphs = join(root, 'assets', 'glyphs');
const pascal = (name) => name.replace(/(^|-)(\w)/g, (_, __, c) => c.toUpperCase());
const entries = readdirSync(glyphs).filter((f) => f.endsWith('.svg')).sort().map((file) => {
  const d = readFileSync(join(glyphs, file), 'utf8').match(/ d="([^"]+)"/)[1];
  return `    <StreamGeometry x:Key="Glyph.${pascal(file.slice(0, -4))}">${d}</StreamGeometry>`;
});
const themes = join(root, 'src', 'Orla', 'Themes');
mkdirSync(themes, { recursive: true });
writeFileSync(join(themes, 'Glyphs.xaml'), `<!-- Generated from assets/glyphs by tools/icons/export.mjs. Edit the SVG sources, not this file. -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
${entries.join('\n')}
</ResourceDictionary>
`);
console.log('Icons exported.');
