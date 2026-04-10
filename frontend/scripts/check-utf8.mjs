import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join } from 'node:path'

const ROOTS = ['src', 'index.html']
const ALLOWED_EXTENSIONS = new Set(['.ts', '.tsx', '.js', '.jsx', '.json', '.css', '.html'])

const decoder = new TextDecoder('utf-8', { fatal: true })
const invalidFiles = []

function walk(path) {
  const stat = statSync(path)
  if (stat.isDirectory()) {
    for (const entry of readdirSync(path)) {
      walk(join(path, entry))
    }
    return
  }

  const dot = path.lastIndexOf('.')
  const ext = dot >= 0 ? path.slice(dot) : ''
  if (!ALLOWED_EXTENSIONS.has(ext)) {
    return
  }

  const bytes = readFileSync(path)
  try {
    decoder.decode(bytes)
  } catch {
    invalidFiles.push(path)
  }
}

for (const root of ROOTS) {
  walk(root)
}

if (invalidFiles.length > 0) {
  console.error('UTF-8 validation failed for:')
  for (const file of invalidFiles) {
    console.error(`- ${file}`)
  }
  process.exit(1)
}

console.log('UTF-8 validation passed.')
