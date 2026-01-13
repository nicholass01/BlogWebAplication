import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { BrowserRouter, Routes, Route, Link, useNavigate, useLocation } from 'react-router-dom';
import './index.css';

type Post = {
  id: string;
  authorId: string;
  authorName: string;
  title: string;
  slug: string;
  content: string;
  createdAtUtc: string;
  publishedAtUtc?: string | null;
  isPublished: boolean;
};

type AuthResult = {
  userId: string;
  username: string;
  token: string;
};

type AuthState = AuthResult | null;

type Toast = { id: number; message: string; type: 'success' | 'error' };

const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5157').replace(/\/$/, '');

function normalizeErrorMessage(status: number, text: string) {
  const trimmed = text?.trim();
  if (!trimmed) {
    if (status === 401) return "Nao autorizado. Faca login novamente.";
    if (status === 403) return "Acesso negado.";
    if (status === 404) return "Recurso nao encontrado.";
    return `Erro ${status}`;
  }

  // If backend returned JSON with validation errors (e.g., { errors: { Title: [...] } })
  try {
    const parsed = JSON.parse(trimmed);
    if (parsed?.message) return parsed.message as string;
    if (parsed?.errors && typeof parsed.errors === 'object') {
      const all = Object.entries(parsed.errors as Record<string, string[] | string>)
        .flatMap(([field, msgs]) => (Array.isArray(msgs) ? msgs.map((m) => `${field}: ${m}`) : [`${field}: ${msgs}`]));
      if (all.length) return `Erros de validacao:\n- ${all.join('\n- ')}`;
    }
  } catch {
    // Ignore JSON parse errors and keep checking text patterns.
  }

  const lower = trimmed.toLowerCase();
  if (lower.includes('validation failed')) {
    const after = trimmed.substring(trimmed.toLowerCase().indexOf('validation failed') + 'validation failed:'.length);
    const items = after
      .split(' - ')
      .map((s) => s.trim())
      .filter((s) => s && !s.toLowerCase().startsWith('end of stack trace') && s.includes(':'))
      .map((s) => s.replace(/^[-\s]+/, '').trim());
    if (items.length) return `Erros de validacao:\n- ${items.join('\n- ')}`;
  }

  if (lower.includes('constraint') || lower.includes('foreign key')) {
    return 'Operacao invalida: verifique os dados relacionados (foreign key).';
  }

  return trimmed;
}
async function apiFetch<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...(options.headers ?? {}),
  };
  if (token) {
    (headers as Record<string, string>).Authorization = `Bearer ${token}`;
  }
  const response = await fetch(`${API_BASE}${path}`, { ...options, headers });
  if (!response.ok) {
    const text = await response.text();
    throw new Error(normalizeErrorMessage(response.status, text));
  }
  if (response.status === 204) return null as T;
  return (await response.json()) as T;
}

function useTheme() {
  const [theme, setTheme] = useState<'light' | 'dark'>(() => {
    const saved = localStorage.getItem('blog-theme');
    return saved === 'dark' || saved === 'light' ? saved : 'light';
  });

  useEffect(() => {
    const root = document.documentElement;
    if (theme === 'dark') root.classList.add('dark');
    else root.classList.remove('dark');
    localStorage.setItem('blog-theme', theme);
  }, [theme]);

  return { theme, setTheme };
}

function useAuthState() {
  const [auth, setAuth] = useState<AuthState>(() => {
    const saved = localStorage.getItem('blog-auth');
    return saved ? (JSON.parse(saved) as AuthResult) : null;
  });
  useEffect(() => {
    if (auth) localStorage.setItem('blog-auth', JSON.stringify(auth));
    else localStorage.removeItem('blog-auth');
  }, [auth]);
  return { auth, setAuth };
}

function useToasts() {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const push = (message: string, type: Toast['type'] = 'success') => {
    const id = Date.now();
    setToasts((prev) => [...prev, { id, message, type }]);
    setTimeout(() => setToasts((prev) => prev.filter((t) => t.id !== id)), 4000);
  };
  return { toasts, push };
}

function Layout({
  children,
  auth,
  onLogout,
  theme,
  toggleTheme,
}: {
  children: React.ReactNode;
  auth: AuthState;
  onLogout: () => void;
  theme: 'light' | 'dark';
  toggleTheme: () => void;
}) {
  const location = useLocation();
  const links = [
    { to: '/', label: 'Posts' },
    { to: '/new', label: 'Novo post' },
  ];

  return (
    <div className="min-h-screen bg-[var(--bg)] text-[var(--text)]">
      <header className="border-b border-[var(--border)] bg-[var(--surface)]">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-4">
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-full bg-[var(--accent-soft)] text-[var(--accent)] flex items-center justify-center font-semibold">B</div>
            <div>
              <div className="text-sm font-semibold text-[var(--muted)]">Blog API</div>
              <div className="text-lg font-semibold">Painel</div>
            </div>
          </div>
          <nav className="flex items-center gap-3 text-sm">
            {links.map((link) => (
              <Link
                key={link.to}
                to={link.to}
                className={`rounded-full px-3 py-2 font-semibold transition ${
                  location.pathname === link.to
                    ? 'bg-[var(--accent)] text-white'
                    : 'text-[var(--muted)] hover:text-[var(--text)] hover:bg-[var(--border)]'
                }`}
              >
                {link.label}
              </Link>
            ))}
            {auth ? (
              <>
                <span className="text-[var(--muted)] px-2">Ola, {auth.username}</span>
                <button
                  onClick={onLogout}
                  className="rounded-full border border-[var(--border)] px-3 py-2 font-semibold text-[var(--muted)] hover:text-[var(--text)] hover:border-[var(--text)]"
                >
                  Sair
                </button>
              </>
            ) : (
              <>
                <Link
                  to="/login"
                  className={`rounded-full px-3 py-2 font-semibold transition ${
                    location.pathname === '/login'
                      ? 'bg-[var(--accent)] text-white'
                      : 'text-[var(--muted)] hover:text-[var(--text)] hover:bg-[var(--border)]'
                  }`}
                >
                  Login
                </Link>
                <Link
                  to="/register"
                  className={`rounded-full px-3 py-2 font-semibold transition ${
                    location.pathname === '/register'
                      ? 'bg-[var(--accent)] text-white'
                      : 'text-[var(--muted)] hover:text-[var(--text)] hover:bg-[var(--border)]'
                  }`}
                >
                  Cadastro
                </Link>
              </>
            )}
            <button
              onClick={toggleTheme}
              className="ml-2 rounded-full border border-[var(--border)] px-3 py-2 text-sm font-semibold text-[var(--muted)] hover:text-[var(--text)] hover:border-[var(--text)]"
            >
              {theme === 'light' ? 'Dark' : 'Light'}
            </button>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-4 py-8">{children}</main>
    </div>
  );
}

function Toasts({ toasts }: { toasts: Toast[] }) {
  return (
    <div className="pointer-events-none fixed right-4 top-4 z-50 space-y-2">
      {toasts.map((t) => (
        <div
          key={t.id}
          className={`pointer-events-auto rounded-xl px-4 py-3 text-sm shadow-panel ${
            t.type === 'success'
              ? 'border border-emerald-200 bg-emerald-50 text-emerald-800'
              : 'border border-red-200 bg-red-50 text-red-700'
          }`}
        >
          {t.message}
        </div>
      ))}
    </div>
  );
}

function PostsPage({ auth, pushToast }: { auth: AuthState; pushToast: (m: string, t?: Toast['type']) => void }) {
  const [posts, setPosts] = useState<Post[]>([]);
  const [selected, setSelected] = useState<Post | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [order, setOrder] = useState<'newest' | 'oldest'>('newest');

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await apiFetch<Post[]>('/api/posts');
        setPosts(data);
        if (data.length > 0) setSelected(data[0]);
      } catch (err) {
        setError((err as Error).message);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    const filteredPosts = term
      ? posts.filter(
          (p) =>
            p.title.toLowerCase().includes(term) ||
            p.slug.toLowerCase().includes(term) ||
            p.content.toLowerCase().includes(term),
        )
      : posts;
    return filteredPosts.sort((a, b) => {
      const aDate = new Date(a.publishedAtUtc ?? a.createdAtUtc).getTime();
      const bDate = new Date(b.publishedAtUtc ?? b.createdAtUtc).getTime();
      return order === 'newest' ? bDate - aDate : aDate - bDate;
    });
  }, [posts, search, order]);

  const publish = async (postId: string) => {
    if (!auth) {
      pushToast('Faca login para publicar', 'error');
      return;
    }
    try {
      await apiFetch(`/api/posts/${postId}/publish`, { method: 'POST' }, auth.token);
      pushToast('Post publicado', 'success');
      const updated = await apiFetch<Post[]>('/api/posts');
      setPosts(updated);
      const sel = updated.find((p) => p.id === postId);
      if (sel) setSelected(sel);
    } catch (err) {
      pushToast((err as Error).message, 'error');
    }
  };

  const skeletonCards = Array.from({ length: 4 });

  return (
    <div className="grid gap-6 lg:grid-cols-10">
      <div className="lg:col-span-4 space-y-3">
        <div className="rounded-2xl border border-[var(--border)] bg-[var(--surface)] p-4 shadow-panel">
          <div className="mb-3 flex items-center justify-between gap-2">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">Posts</div>
              <div className="text-lg font-semibold">Publicados</div>
            </div>
            <Link
              to="/new"
              className="rounded-full bg-[var(--accent)] px-3 py-2 text-sm font-semibold text-white hover:brightness-95"
            >
              Novo post
            </Link>
          </div>
          <div className="flex gap-2 mb-3">
            <input
              className="w-full rounded-xl border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm outline-none focus:border-[var(--accent)]"
              placeholder="Buscar por titulo, slug ou conteudo..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <select
              className="rounded-xl border border-[var(--border)] bg-[var(--surface)] px-2 py-2 text-sm outline-none focus:border-[var(--accent)]"
              value={order}
              onChange={(e) => setOrder(e.target.value as 'newest' | 'oldest')}
            >
              <option value="newest">Mais recentes</option>
              <option value="oldest">Mais antigos</option>
            </select>
          </div>
                    {loading && (
            <div className="space-y-2">
              {skeletonCards.map((_, idx) => (
                <div key={idx} className="rounded-xl border border-[var(--border)] bg-[var(--surface)] p-3">
                  <div className="h-4 w-1/3 animate-pulse rounded bg-[var(--border)]" />
                  <div className="mt-2 h-3 w-1/2 animate-pulse rounded bg-[var(--border)]" />
                  <div className="mt-2 h-3 w-5/6 animate-pulse rounded bg-[var(--border)]" />
                </div>
              ))}
            </div>
          )}
          {error && <p className="text-sm text-red-500">{error}</p>}
          {!loading && filtered.length === 0 && <p className="text-sm text-[var(--muted)]">Nenhum post.</p>}
          <div className="space-y-2">
            {filtered.map((post) => (
              <button
                key={post.id}
                onClick={() => setSelected(post)}
                className={`w-full rounded-xl border px-3 py-3 text-left transition ${
                  selected?.id === post.id
                    ? 'border-[var(--accent)] bg-[var(--accent-soft)]'
                    : 'border-[var(--border)] hover:border-[var(--accent)] hover:bg-[var(--accent-soft)]/50'
                }`}
              >
                <div className="flex items-center justify-between">
                  <div className="text-base font-semibold">{post.title}</div>
                  <span
                    className={`rounded-full px-2 py-1 text-[11px] font-semibold ${
                      post.isPublished ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'
                    }`}
                  >
                    {post.isPublished ? 'Publicado' : 'Rascunho'}
                  </span>
                </div>
                <div className="text-xs uppercase tracking-[0.15em] text-[var(--muted)]">{post.slug}</div>
                <div className="mt-1 line-clamp-2 text-sm text-[var(--muted)]">{post.content}</div>
              </button>
            ))}
          </div>
        </div>
      </div>
      <div className="lg:col-span-6">
        <div className="rounded-2xl border border-[var(--border)] bg-[var(--surface)] p-6 shadow-panel min-h-[320px]">
          {!selected && <p className="text-sm text-[var(--muted)]">Selecione um post para ver os detalhes.</p>}
          {selected && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Detalhe</div>
                  <div className="text-2xl font-semibold">{selected.title}</div>
                  <div className="text-xs uppercase tracking-[0.15em] text-[var(--muted)]">{selected.slug}</div>
                </div>
                <div className="flex items-center gap-2">
                  {!selected.isPublished && (
                    <button
                      onClick={() => publish(selected.id)}
                      className="rounded-full bg-[var(--accent)] px-3 py-1 text-xs font-semibold text-white hover:brightness-95"
                    >
                      Publicar
                    </button>
                  )}
                </div>
              </div>
              <p className="whitespace-pre-wrap text-sm leading-6 text-[var(--text)]">{selected.content}</p>
              <div className="flex flex-wrap gap-3 text-xs text-[var(--muted)]">
                <span>Autor: {selected.authorName?.trim() || selected.authorId}</span>
                <span>Slug: {selected.slug}</span>
                <span>
                  Publicado:{' '}
                  {selected.publishedAtUtc ? new Date(selected.publishedAtUtc).toLocaleString() : 'Rascunho'}
                </span>
              </div>
              {!auth && <p className="text-xs text-[var(--muted)]">Entre para criar ou editar posts.</p>}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function LoginPage({ onAuth }: { onAuth: (auth: AuthResult) => void }) {
  const navigate = useNavigate();
  const [form, setForm] = useState({ username: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const result = await apiFetch<AuthResult>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ usernameOrEmail: form.username, password: form.password }),
      });
      onAuth(result);
      navigate('/');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthCard
      title="Entrar"
      subtitle="Acesse para publicar e gerenciar posts."
      onSubmit={handleSubmit}
      loading={loading}
      error={error}
      footer={
        <p className="text-sm text-[var(--muted)]">
          Nao tem conta?{' '}
          <Link to="/register" className="font-semibold text-[var(--accent)] hover:underline">
            Cadastre-se
          </Link>
        </p>
      }
    >
      <Label>Usuario ou e-mail</Label>
      <Input
        value={form.username}
        onChange={(e) => setForm({ ...form, username: e.target.value })}
        required
        placeholder="seu-usuario"
      />
      <Label>Senha</Label>
      <Input
        type="password"
        value={form.password}
        onChange={(e) => setForm({ ...form, password: e.target.value })}
        required
        placeholder="********"
      />
    </AuthCard>
  );
}

function RegisterPage({ onAuth }: { onAuth: (auth: AuthResult) => void }) {
  const navigate = useNavigate();
  const [form, setForm] = useState({ username: '', email: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const result = await apiFetch<AuthResult>('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ ...form, isAdmin: false }),
      });
      onAuth(result);
      navigate('/');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthCard
      title="Criar conta"
      subtitle="Comece a publicar seus posts."
      onSubmit={handleSubmit}
      loading={loading}
      error={error}
      footer={
        <p className="text-sm text-[var(--muted)]">
          Ja tem conta?{' '}
          <Link to="/login" className="font-semibold text-[var(--accent)] hover:underline">
            Entre aqui
          </Link>
        </p>
      }
    >
      <Label>Usuario</Label>
      <Input
        value={form.username}
        onChange={(e) => setForm({ ...form, username: e.target.value })}
        required
        placeholder="seu-usuario"
      />
      <Label>E-mail</Label>
      <Input
        type="email"
        value={form.email}
        onChange={(e) => setForm({ ...form, email: e.target.value })}
        required
        placeholder="email@exemplo.com"
      />
      <Label>Senha</Label>
      <Input
        type="password"
        value={form.password}
        onChange={(e) => setForm({ ...form, password: e.target.value })}
        required
        placeholder="********"
      />
    </AuthCard>
  );
}

function CreatePostPage({ auth }: { auth: AuthState }) {
  const navigate = useNavigate();
  const [form, setForm] = useState({ title: '', content: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!auth) {
      setError('Faca login para publicar.');
      return;
    }
    setLoading(true);
    setError(null);
    setSuccess(null);
    try {
      await apiFetch<{ id: string }>('/api/posts', {
        method: 'POST',
        body: JSON.stringify({ ...form, publish: true }),
      }, auth.token);
      setSuccess('Post criado com sucesso.');
      setForm({ title: '', content: '' });
      navigate('/');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-3xl rounded-2xl border border-[var(--border)] bg-[var(--surface)] p-6 shadow-panel">
      <div className="mb-4">
        <div className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">Novo</div>
        <h1 className="text-2xl font-semibold">Criar post</h1>
        <p className="text-sm text-[var(--muted)]">Escreva e publique imediatamente.</p>
      </div>
      {error && <div className="mb-3 rounded-xl border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">{error}</div>}
      {success && <div className="mb-3 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm text-emerald-700">{success}</div>}
      <form className="space-y-3" onSubmit={handleSubmit}>
        <div>
          <Label>Titulo</Label>
          <Input
            value={form.title}
            onChange={(e) => setForm({ ...form, title: e.target.value })}
            required
            placeholder="Minha novidade"
          />
        </div>
        <div>
          <Label>Conteudo</Label>
          <textarea
            className="w-full rounded-xl border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm outline-none focus:border-[var(--accent)]"
            rows={6}
            value={form.content}
            onChange={(e) => setForm({ ...form, content: e.target.value })}
            required
            placeholder="Escreva seu post aqui..."
          />
        </div>
        <div className="flex gap-3">
          <button
            type="submit"
            disabled={loading}
            className="rounded-full bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white hover:brightness-95 disabled:opacity-60"
          >
            {loading ? 'Enviando...' : 'Publicar'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/')}
            className="rounded-full border border-[var(--border)] px-4 py-2 text-sm font-semibold text-[var(--muted)] hover:text-[var(--text)] hover:border-[var(--text)]"
          >
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

function AuthCard({
  title,
  subtitle,
  onSubmit,
  loading,
  error,
  footer,
  children,
}: {
  title: string;
  subtitle: string;
  onSubmit: (e: FormEvent) => void;
  loading: boolean;
  error: string | null;
  footer?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <div className="mx-auto max-w-md space-y-4 rounded-2xl border border-[var(--border)] bg-[var(--surface)] p-6 shadow-panel">
      <div>
        <div className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted)]">Acesso</div>
        <h1 className="text-2xl font-semibold">{title}</h1>
        <p className="text-sm text-[var(--muted)]">{subtitle}</p>
      </div>
      {error && <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">{error}</div>}
      <form className="space-y-3" onSubmit={onSubmit}>
        {children}
        <button
          type="submit"
          disabled={loading}
          className="mt-2 w-full rounded-full bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white hover:brightness-95 disabled:opacity-60"
        >
          {loading ? 'Enviando...' : title}
        </button>
      </form>
      {footer}
    </div>
  );
}

function Label({ children }: { children: React.ReactNode }) {
  return <label className="text-sm font-semibold text-[var(--text)]">{children}</label>;
}

function Input(props: React.InputHTMLAttributes<HTMLInputElement>) {
  const { className, ...rest } = props;
  return (
    <input
      {...rest}
      className={`w-full rounded-xl border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm outline-none focus:border-[var(--accent)] ${className ?? ''}`}
    />
  );
}

function App() {
  const { theme, setTheme } = useTheme();
  const { auth, setAuth } = useAuthState();
  const { toasts, push } = useToasts();

  const toggleTheme = () => setTheme(theme === 'light' ? 'dark' : 'light');
  const handleLogout = () => setAuth(null);

  return (
    <BrowserRouter>
      <Layout auth={auth} onLogout={handleLogout} theme={theme} toggleTheme={toggleTheme}>
        <Routes>
          <Route path="/" element={<PostsPage auth={auth} pushToast={push} />} />
          <Route path="/login" element={<LoginPage onAuth={setAuth} />} />
          <Route path="/register" element={<RegisterPage onAuth={setAuth} />} />
          <Route path="/new" element={<CreatePostPage auth={auth} />} />
        </Routes>
      </Layout>
      <Toasts toasts={toasts} />
    </BrowserRouter>
  );
}

export default App;























