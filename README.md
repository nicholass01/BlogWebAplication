# Blog API & Frontend

Aplicação full-stack de blog com backend em ASP.NET Core (Minimal APIs) e frontend em React/Vite. O backend usa PostgreSQL e concentra a persistência em stored procedures (escrita) e functions (leitura), reduzindo SQL espalhado no código e padronizando o acesso a dados.

## Visão geral
- **Backend:** .NET 9, camadas Domain/Application/Infrastructure + BlogWebApp (Minimal APIs).
- **Banco:** PostgreSQL, schema `blog`, rotinas `sp_*` (procedures) e `fn_*` (functions).
- **Frontend:** React + Vite + TypeScript, rotas para posts, login, cadastro e criação de posts.
- **Autenticação:** JWT (Bearer Token). Rotas protegidas para criar/editar/publicar/deletar.

## Estrutura de pastas
- `Blog.Domain` — Entidades e regras de domínio.
- `Blog.Application` — DTOs, validações (FluentValidation), serviços/casos de uso, contratos (`I*Repository`).
- `Blog.Infrastructure` — Acesso a dados (Dapper + Npgsql), repositórios e connection factory.
- `BlogWebApp` — Minimal APIs, DI, CORS, autenticação/autorização, endpoints.
- `database/001_create_schema.sql` — Script inicial (schema/tabelas/procedures/functions).
- `frontend/` — Aplicação React/Vite.

## Banco de dados (PostgreSQL)
Schema `blog` criado por `database/001_create_schema.sql`.

### Tabelas (mínimo)
- `blog.users`  
  `id uuid (PK)`, `name text`, `email text (único)`, `password_hash text`, `created_at timestamptz`
- `blog.posts`  
  `id uuid (PK)`, `author_id uuid (FK users)`, `title text`, `slug text (único)`, `content text`,  
  `is_published boolean`, `published_at timestamptz`, `created_at_utc timestamptz`, `updated_at_utc timestamptz`
- `blog.comments` (opcional)  
  `id uuid (PK)`, `post_id uuid (FK posts)`, `author_name text`, `content text`, `created_at timestamptz`

### Procedures / Functions principais
- **Usuários**
  - `blog.sp_create_user(IN p_name, IN p_email, IN p_password_hash, OUT p_user_id uuid)`
  - `blog.fn_get_user_by_email(p_email text)`
  - `blog.fn_get_user_by_username(p_username text)` (se aplicado)

- **Posts**
  - `blog.sp_create_post(IN p_author_id uuid, IN p_title text, IN p_slug text, IN p_content text, OUT p_post_id uuid)`
  - `blog.sp_update_post(IN p_post_id uuid, IN p_title text, IN p_content text)`
  - `blog.sp_publish_post(IN p_post_id uuid)`
  - `blog.sp_delete_post(IN p_post_id uuid)`
  - `blog.fn_list_posts(p_only_published boolean, p_page int, p_page_size int)` (quando presente)
  - `blog.fn_get_post_by_slug(p_slug text)`

- **Comentários (opcional)**
  - `blog.sp_create_comment(IN p_post_id, IN p_author_name, IN p_content, OUT p_comment_id uuid)`
  - `blog.fn_list_comments(p_post_id uuid)`

### Convenções
- Escrita: `sp_*` (procedures)  
- Leitura: `fn_*` (functions)  
- Datas: preferir colunas `*_utc`; cair para colunas sem `_utc` se o schema não tiver os campos.

### Observação sobre nomes no código
O repositório atual (`PostgresPostRepository`) chama `list_published_posts()`, `get_post_by_id(@p_id)` e `get_post_by_slug(@p_slug)`. Garanta que essas functions existam ou crie wrappers chamando as `fn_*` do script. Exemplo de wrapper `list_published_posts`:
```sql
create or replace function blog.list_published_posts()
returns table (
  id uuid,
  author_id uuid,
  title text,
  slug text,
  is_published boolean,
  published_at timestamptz,
  created_at timestamptz,
  updated_at timestamptz
)
language sql
stable
as $$
  select *
  from blog.fn_list_posts(true, 1, 50);
$$;
```

## Backend (API)

### Repositórios
- `PostgresPostRepository` (Dapper)
  - leitura via `list_published_posts()`, `get_post_by_id(@p_id)`, `get_post_by_slug(@p_slug)` (ou as `fn_*` equivalentes)
  - escrita via `sp_create_post`, `sp_update_post`, `sp_publish_post`, `sp_delete_post`
- `PostgresUserRepository` (Dapper)
  - cadastro via `sp_create_user`
  - login via `fn_get_user_by_email` / `fn_get_user_by_username`

### Endpoints principais (`BlogWebApp/Program.cs`)
- `GET /` — health (`{ "status": "ok" }`)
- `GET /api/posts` — lista publicados (paginável se implementado)
- `GET /api/posts/{slug}` — detalhe por slug
- `POST /api/posts` — cria post (autenticado)  
  Body: `{ "title": "...", "content": "...", "publish": true/false }`
- `PUT /api/posts/{id}` — atualiza post (autenticado)
- `POST /api/posts/{id}/publish` — publica (autenticado)
- `DELETE /api/posts/{id}` — remove (autenticado)
- `POST /api/auth/register` — cria usuário, retorna token
- `POST /api/auth/login` — login, retorna token

### Validação e erros
- FluentValidation valida `title` e `content`.
- Respostas típicas: `400` (validação), `401` (sem token), `404` (não encontrado), `500` (erro interno).

### CORS (dev)
Política liberando origens do frontend:
- `http://localhost:5173`
- `http://localhost:4173`  
(outras URLs podem ser adicionadas conforme necessidade).

## Frontend (Vite/React)
- Rotas: `/` (lista/detalhe), `/login`, `/register`, `/new`
- Tema claro/escuro (toggle), busca e ordenação de posts, toasts
- Exibe `AuthorName` (fallback `AuthorId` se vier vazio)

### Variáveis de ambiente
Arquivo: `frontend/.env`
- `VITE_API_BASE_URL=https://localhost:7047`

### Build/execução
```bash
cd frontend
npm install
npm run dev
```
(Em Windows, evite caminhos com `#` no diretório do projeto Vite, pois quebra a resolução de arquivos.)

## Execução local (passo a passo)
Prérequisitos: PostgreSQL, .NET 9 SDK, Node.js (LTS).

1) Banco  
   - Crie o banco (ex.: `blogdb`).  
   - Aplique `database/001_create_schema.sql`.  
   - (Opcional) crie usuário dedicado e permissões.

2) Backend  
   ```bash
   dotnet restore
   dotnet run --project BlogWebApp/BlogWebApp.csproj --launch-profile https
   ```

3) Frontend  
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

## Autenticação
- JWT emitido em `/api/auth/register` e `/api/auth/login`.
- Use `Authorization: Bearer <token>` nas rotas protegidas.

## Próximos passos sugeridos
- Versionar scripts de banco (002_..., 003_...) e definir migração (Flyway/Liquibase).
- Padronizar paginação/filtros na listagem.
- Testes de integração cobrindo: register → login → create post → publish → list.
