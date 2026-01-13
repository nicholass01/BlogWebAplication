-- Schema and stored routines for the blog API.

create schema if not exists blog;

create table if not exists blog.users (
    id uuid primary key,
    username text not null unique,
    email text not null unique,
    password_hash text not null,
    role text not null check (role in ('admin', 'author')),
    created_at_utc timestamptz not null default now()
);

create table if not exists blog.posts (
    id uuid primary key,
    author_id uuid not null references blog.users(id),
    title text not null,
    slug text not null unique,
    content text not null,
    created_at_utc timestamptz not null default now(),
    updated_at_utc timestamptz not null default now(),
    published_at_utc timestamptz null,
    is_published boolean not null default false
);

create or replace function blog.list_published_posts()
returns setof blog.posts
language sql
as $$
    select *
    from blog.posts
    where is_published
    order by coalesce(published_at_utc, created_at_utc) desc;
$$;

create or replace function blog.get_post_by_id(p_id uuid)
returns blog.posts
language sql
as $$
    select *
    from blog.posts
    where id = p_id;
$$;

create or replace function blog.get_post_by_slug(p_slug text)
returns blog.posts
language sql
as $$
    select *
    from blog.posts
    where slug = p_slug;
$$;

create or replace procedure blog.create_post(
    p_id uuid,
    p_author_id uuid,
    p_title text,
    p_slug text,
    p_content text,
    p_publish boolean
)
language plpgsql
as $$
begin
    insert into blog.posts (id, author_id, title, slug, content, created_at_utc, updated_at_utc, published_at_utc, is_published)
    values (p_id, p_author_id, p_title, p_slug, p_content, now(), now(), case when p_publish then now() end, p_publish);
end;
$$;

create or replace procedure blog.update_post(
    p_id uuid,
    p_title text,
    p_slug text,
    p_content text,
    p_publish boolean
)
language plpgsql
as $$
begin
    update blog.posts
    set title = p_title,
        slug = p_slug,
        content = p_content,
        updated_at_utc = now(),
        is_published = p_publish or is_published,
        published_at_utc = case
            when p_publish and published_at_utc is null then now()
            else published_at_utc
        end
    where id = p_id;
end;
$$;

create or replace procedure blog.delete_post(p_id uuid)
language plpgsql
as $$
begin
    delete from blog.posts where id = p_id;
end;
$$;

create or replace procedure blog.set_post_publication(p_id uuid, p_publish boolean)
language plpgsql
as $$
begin
    update blog.posts
    set is_published = p_publish,
        published_at_utc = case
            when p_publish and published_at_utc is null then now()
            when not p_publish then null
            else published_at_utc
        end
    where id = p_id;
end;
$$;

create or replace function blog.get_user_by_username(p_username text)
returns blog.users
language sql
as $$
    select *
    from blog.users
    where username = p_username;
$$;

create or replace function blog.get_user_by_email(p_email text)
returns blog.users
language sql
as $$
    select *
    from blog.users
    where email = p_email;
$$;

create or replace procedure blog.create_user(
    p_id uuid,
    p_username text,
    p_email text,
    p_password_hash text,
    p_role text
)
language plpgsql
as $$
begin
    insert into blog.users (id, username, email, password_hash, role, created_at_utc)
    values (p_id, p_username, p_email, p_password_hash, p_role, now());
end;
$$;
