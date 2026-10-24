// PostToolUse (Edit|Write|MultiEdit): formata com Prettier o arquivo do front que acabou de ser editado.
// O backend é verificado por `dotnet format --verify-no-changes` no CI (rodar a cada edição seria lento).
import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { relative, resolve } from 'node:path';

const input = JSON.parse(readFileSync(0, 'utf8'));
const filePath = String(input?.tool_input?.file_path ?? '');
const frontend = resolve(input?.cwd ?? process.cwd(), 'frontend');
const relativePath = relative(frontend, resolve(filePath)).replaceAll('\\', '/');

const insideFrontend = relativePath !== '' && !relativePath.startsWith('..');
const formattable = /\.(ts|html|scss|json|md)$/.test(relativePath);
const generated = relativePath.startsWith('src/app/api/');

if (insideFrontend && formattable && !generated) {
  try {
    const prettier = resolve(frontend, 'node_modules/prettier/bin/prettier.cjs');
    execFileSync(process.execPath, [prettier, '--write', relativePath], {
      cwd: frontend,
      stdio: 'ignore',
    });
  } catch {
    // Formatação é conveniência: falha aqui não bloqueia a edição; o lint do CI pega o resto.
  }
}
