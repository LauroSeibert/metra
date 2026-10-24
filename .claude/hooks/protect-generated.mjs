// PreToolUse (Edit|Write|MultiEdit): bloqueia edição manual de arquivos gerados.
// - frontend/src/app/api/**: cliente OpenAPI, regenere com `npm run api:generate`.
// - backend/**/Migrations/**: migrations são criadas pelo `dotnet ef` e nunca editadas depois de commitadas.
// - backend/openapi/**: contrato gerado no build da API.
// Saída 2 bloqueia a ferramenta e devolve a mensagem ao Claude.
import { readFileSync } from 'node:fs';

const input = JSON.parse(readFileSync(0, 'utf8'));
const filePath = String(input?.tool_input?.file_path ?? '').replaceAll('\\', '/');

const rules = [
  {
    pattern: /\/frontend\/src\/app\/api\//,
    message: 'Arquivo gerado pelo ng-openapi-gen. Altere a API e rode `npm run api:generate` em frontend/.',
  },
  {
    pattern: /\/backend\/.*\/Migrations\/(?!\.editorconfig$)/,
    message:
      'Migration gerada pelo dotnet ef. Crie uma nova com `dotnet ef migrations add` em vez de editar uma existente.',
  },
  {
    pattern: /\/backend\/openapi\//,
    message: 'Contrato OpenAPI gerado no build da API. Altere os endpoints/DTOs e rode `dotnet build`.',
  },
];

const match = rules.find((rule) => rule.pattern.test(filePath));
if (match) {
  process.stderr.write(`Edição bloqueada em ${filePath}: ${match.message}\n`);
  process.exit(2);
}
