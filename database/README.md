# PostgreSQL scripts

Versione o banco criando arquivos SQL numerados neste diretório. Os repositórios usam apenas stored procedures/functions (schema `blog`) para acesso:

- `blog.create_user`, `blog.get_user_by_username`, `blog.get_user_by_email`
- `blog.create_post`, `blog.update_post`, `blog.delete_post`, `blog.set_post_publication`, `blog.get_post_by_id`, `blog.get_post_by_slug`, `blog.list_published_posts`

Para aplicar em desenvolvimento:

```bash
psql "$POSTGRES_CONNECTION" -f database/001_create_schema.sql
```

Substitua `$POSTGRES_CONNECTION` por sua connection string (igual a `ConnectionStrings:DefaultConnection`).
