# Blog API & Frontend

Aplicação full-stack de blog com backend em ASP.NET Core (Minimal APIs) e frontend em React/Vite. O backend usa PostgreSQL e concentra a persistência em **stored procedures** (escrita) e **functions** (leitura), reduzindo SQL espalhado no código e padronizando o acesso a dados.

## Visão geral

- **Backend:** .NET 9, arquitetura em camadas `Domain / Application / Infrastructure` + `BlogWebApp` (Minimal APIs).
- **Banco:** PostgreSQL, schema `blog`, rotinas `sp_*` (procedures) e `fn_*` (functions).
- **Frontend:** React + Vite + TypeScript, rotas para posts, login, cadastro e criação de posts.
- **Autenticação:** JWT (Bearer Token). Rotas protegidas para criar/editar/publicar/deletar.

## Estrutura de pastas

- `Blog.Domain` — Entidades e regras de domínio (sem dependência de banco/framework).
- `Blog.Application` — DTOs, validações (FluentValidation), serviços/casos de uso, contratos (`I*Repository`).
- `Blog.Infrastructure` — Acesso a dados (Dapper + Npgsql), repositórios e connection factory.
- `BlogWebApp` — Minimal APIs, DI, CORS, autenticação/autorizações, endpoints.
- `database/001_create_schema.sql` — Script inicial (schema/tabelas/procedures/functions).
- `frontend/` — Aplicação React/Vite.

---

## Banco de dados (PostgreSQL)

O schema `blog` é criado por `database/001_create_schema.sql`.

### Tabelas (mínimo)

- `blog.users`
  - `id uuid` (PK)
  - `name text`
  - `email text` (único)
  - `password_hash text`
  - `created_at timestamptz`

- `blog.posts`
  - `id uuid` (PK)
  - `author_id uuid` (FK `blog.users`)
  - `title text`
  - `slug text` (único)
  - `content text`
  - `is_published boolean`
  - `published_at timestamptz`
  - `created_at timestamptz`
  - `updated_at timestamptz`

- `blog.comments` (se habilitado no script)
  - `id uuid` (PK)
  - `post_id uuid` (FK `blog.posts`)
  - `author_name text`
  - `content text`
  - `created_at timestamptz`

### Procedures / Functions principais

#### Usuários
- `blog.sp_create_user(IN p_name, IN p_email, IN p_password_hash, OUT p_user_id uuid)`
- `blog.fn_get_user_by_email(p_email text)`

#### Posts
- `blog.sp_create_post(IN p_author_id uuid, IN p_title text, IN p_slug text, IN p_content text, OUT p_post_id uuid)`
- `blog.sp_update_post(IN p_post_id uuid, IN p_title text, IN p_content text)`
- `blog.sp_publish_post(IN p_post_id uuid)`
- `blog.sp_delete_post(IN p_post_id uuid)`
- `blog.fn_list_posts(p_only_published boolean, p_page int, p_page_size int)`
- `blog.fn_get_post_by_slug(p_slug text)`

#### Comentários (opcional)
- `blog.sp_create_comment(IN p_post_id, IN p_author_name, IN p_content, OUT p_comment_id uuid)`
- `blog.fn_list_comments(p_post_id uuid)`

### Convenções
- Escrita: `sp_*` (procedures)
- Leitura: `fn_*` (functions)

### Observação importante sobre nomes usados no código
Se o repositório estiver chamando `blog.list_published_posts()` (ou outros nomes antigos), você tem duas opções:

1) **Recomendado:** ajustar o código para chamar as rotinas do script (`blog.fn_list_posts(true, @page, @pageSize)` etc.)
2) **Compatibilidade:** criar uma function “wrapper” com o nome que o código espera.

Exemplo de wrapper para `list_published_posts()`:
```sql
CREATE OR REPLACE FUNCTION blog.list_published_posts()
RETURNS TABLE (
  id uuid,
  author_id uuid,
  title text,
  slug text,
  is_published boolean,
  published_at timestamptz,
  created_at timestamptz,
  updated_at timestamptz
)
LANGUAGE sql
STABLE
AS $$
  SELECT *
  FROM blog.fn_list_posts(true, 1, 50);
$$;
```

---

## Backend (API)

### Repositórios
- `PostgresPostRepository` (Dapper):
  - leitura via functions (`fn_list_posts`, `fn_get_post_by_slug`) ou wrappers (se existir)
  - escrita via procedures (`sp_create_post`, `sp_update_post`, `sp_publish_post`, `sp_delete_post`)
- `PostgresUserRepository` (Dapper):
  - cadastro via `sp_create_user`
  - login via `fn_get_user_by_email`

### Endpoints principais (`BlogWebApp/Program.cs`)
- `GET /` — health check (`{ "status": "ok" }`)
- `GET /api/posts` — lista posts publicados (paginável, se implementado)
- `GET /api/posts/{slug}` — detalhes por slug
- `POST /api/posts` — cria post (autenticado)  
  Body: `{ "title": "...", "content": "...", "publish": true/false }`
- `PUT /api/posts/{id}` — atualiza post (autenticado)
- `POST /api/posts/{id}/publish` — publica (autenticado)
- `DELETE /api/posts/{id}` — remove (autenticado)
- `POST /api/auth/register` — cadastra usuário, retorna token
- `POST /api/auth/login` — login, retorna token

### Validação e erros
- FluentValidation valida `title` e `content`.
- Respostas típicas: `400` (validação), `401` (sem token), `404` (não encontrado), `500` (erro interno).

### CORS (dev)
Política liberando origens do frontend:
- `http://localhost:5173`
- `http://localhost:4173`

> Importante: sem CORS, o navegador falha no preflight `OPTIONS` e você verá `405 Method Not Allowed`.

---

## Frontend (Vite/React)

- Rotas: `/` (lista/detalhe), `/login`, `/register`, `/new`
- Tema claro/escuro (toggle)
- Busca e ordenação de posts
- Toasts para mensagens

### Variáveis de ambiente
Arquivo: `frontend/.env`
- `VITE_API_BASE_URL=https://localhost:7047`

### Build/execução
```bash
cd frontend
npm install
npm run dev
```

> Nota (Windows): evite rodar o Vite em caminho com caractere `#` (ex.: pasta `C#`), pois pode quebrar resolução de arquivos. Prefira `CSharp` ou similar.

---

## Execução local (passo a passo)

### Pré-requisitos
- PostgreSQL (local)
- .NET 9 SDK
- Node.js (LTS recomendado)

### 1) Banco
- Crie o banco `blogdb` (se ainda não existir).
- Aplique o script `database/001_create_schema.sql` no pgAdmin (Query Tool) ou via `psql`.
- (Opcional) crie o usuário `blog_user` e permissões se você não quiser usar `postgres` na API.

### 2) Backend
```bash
dotnet restore
dotnet run --project BlogWebApp/BlogWebApp.csproj --launch-profile https
```

### 3) Frontend
```bash
cd frontend
npm install
npm run dev
```

---

## Autenticação

- JWT emitido em `/api/auth/register` e `/api/auth/login`
- Envie o token nas rotas protegidas:
  - `Authorization: Bearer <token>`

---

## Próximos passos sugeridos

- Versionar scripts de banco (ex.: `002_...sql`, `003_...sql`) e definir um processo de migração.
- Padronizar paginação e filtros na listagem.
- Testes de integração cobrindo: register → login → create post → publish → list.
