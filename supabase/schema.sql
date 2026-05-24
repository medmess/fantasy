create table if not exists public.fantasy_groups (
  id text primary key,
  code text not null unique,
  name text not null,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  members integer not null default 1,
  max_members integer not null default 7,
  created_at timestamptz not null default now()
);

create table if not exists public.fantasy_group_members (
  group_id text not null references public.fantasy_groups(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  joined_at timestamptz not null default now(),
  primary key (group_id, user_id)
);

alter table public.fantasy_groups enable row level security;
alter table public.fantasy_group_members enable row level security;

create policy "members can read groups"
on public.fantasy_groups
for select
using (
  exists (
    select 1
    from public.fantasy_group_members m
    where m.group_id = id and m.user_id = auth.uid()
  )
);

create policy "members can read memberships"
on public.fantasy_group_members
for select
using (user_id = auth.uid());

create table if not exists public.news_posts (
  id text primary key,
  telegram_post_id bigint not null unique,
  caption text not null,
  image_path text not null,
  image_url text,
  source text not null default 'Offside',
  published_at timestamptz not null,
  created_at timestamptz not null default now()
);

create index if not exists news_posts_published_at_idx
on public.news_posts (published_at desc);

alter table public.news_posts enable row level security;

create policy "news posts are readable"
on public.news_posts
for select
using (true);

create table if not exists public.news_ads (
  id text primary key,
  title text not null,
  subtitle text,
  image_url text not null,
  target_url text,
  placement text not null default 'news_feed',
  is_active boolean not null default true,
  created_at timestamptz not null default now()
);

create index if not exists news_ads_active_created_at_idx
on public.news_ads (is_active, created_at desc);

alter table public.news_ads enable row level security;

create policy "active news ads are readable"
on public.news_ads
for select
using (is_active = true);
