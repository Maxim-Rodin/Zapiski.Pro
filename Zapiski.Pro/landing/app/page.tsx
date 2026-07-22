"use client"

import { motion, useScroll, useTransform } from "framer-motion"
import {
  ArrowRight, BarChart3, Bell, BriefcaseBusiness, CalendarDays, Check, CheckCircle2, ChevronRight,
  Clock3, Instagram, MapPin, Menu, MessageCircle, MousePointer2, PhoneCall, Plus, Send, Sparkles,
  Star, UserRound, UsersRound, WandSparkles,
} from "lucide-react"
import { useRef, useState } from "react"
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion"
import { Button } from "@/components/ui/button"

const TELEGRAM_URL = "https://t.me/ZapisiProBot?start=register_master_landing"

const reveal = { initial: { opacity: 0, y: 28 }, whileInView: { opacity: 1, y: 0 }, viewport: { once: true, margin: "-80px" }, transition: { duration: .65 } }

function Logo() {
  return <a className="logo" href="#top" aria-label="Zapisi Pro — на главную"><span><BriefcaseBusiness size={19} strokeWidth={2.5} /></span><b>Zapisi.Pro</b></a>
}

function Phone({ compact = false }: { compact?: boolean }) {
  return (
    <div className={`phone ${compact ? "phone-compact" : ""}`}>
      <div className="phone-speaker" />
      <div className="telegram-bar"><ChevronRight size={19} /><span>Zapisi Pro</span><div className="tg-mini"><span /><span /><span /></div></div>
      <div className="miniapp">
        <div className="real-app-header">
          <div className="app-mark"><BriefcaseBusiness /></div>
          <div><strong>Zapisi.Pro</strong><small>кабинет мастера</small></div>
          <button>i</button><button className="hamb"><i /><i /><i /></button>
        </div>
        <div className="master-hello"><div><strong>Привет!</strong><span>@anna_nails, это ваша мастер-панель</span><button>Мои записи ›</button></div><div className="briefcase">▱</div></div>
        <div className="subscription-ok"><span><Check /></span><div><strong>Подписка активна</strong><small>Осталось 26 дн. Приятного пользования</small></div></div>
        <div className="client-cabinet"><span><UserRound /></span><div><strong>Клиентский кабинет</strong><small>Ваши записи и мастера</small></div><ChevronRight /></div>
        <div className="master-link"><div><strong>Ваша ссылка</strong><small>t.me/ZapisiProBot?start=anna</small></div><button>Скопировать</button></div>
        <div className="master-tiles"><div><span><CalendarDays /></span><strong>Записи</strong><small>Сегодня 5 записей</small></div><div><span><UsersRound /></span><strong>Клиенты</strong><small>База клиентов мастера</small></div></div>
        <nav className="mini-nav"><span className="selected"><CalendarDays /><small>Главная</small></span><span><Clock3 /><small>Записи</small></span><button><Plus /></button><span><UsersRound /><small>Клиенты</small></span><span><UserRound /><small>Профиль</small></span></nav>
      </div>
    </div>
  )
}

function SectionTitle({ eyebrow, title, text, light = false }: { eyebrow: string; title: string; text?: string; light?: boolean }) {
  return <motion.div className={`section-title ${light ? "light" : ""}`} {...reveal}><span className="eyebrow">{eyebrow}</span><h2>{title}</h2>{text && <p>{text}</p>}</motion.div>
}

export default function Home() {
  const heroRef = useRef<HTMLDivElement>(null)
  const { scrollYProgress } = useScroll({ target: heroRef, offset: ["start start", "end start"] })
  const phoneY = useTransform(scrollYProgress, [0, 1], [0, 42])
  const [menuOpen, setMenuOpen] = useState(false)

  return (
    <main id="top">
      <header className="site-header">
        <Logo />
        <nav className={menuOpen ? "open" : ""}>
          <a href="#features" onClick={() => setMenuOpen(false)}>Возможности</a><a href="#how" onClick={() => setMenuOpen(false)}>Как работает</a><a href="#pricing" onClick={() => setMenuOpen(false)}>Тарифы</a><a href="#faq" onClick={() => setMenuOpen(false)}>Вопросы</a>
        </nav>
        <Button size="sm" asChild><a href={TELEGRAM_URL} target="_blank" rel="noopener noreferrer">Попробовать <ArrowRight size={16} /></a></Button>
        <button className="menu-button" onClick={() => setMenuOpen(!menuOpen)} aria-label="Открыть меню"><Menu /></button>
      </header>

      <section className="hero-section" ref={heroRef}>
        <div className="hero-glow" />
        <div className="hero-copy">
          <motion.div className="hero-badge" initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}><Send size={15} fill="currentColor" /> Онлайн-запись внутри Telegram</motion.div>
          <motion.h1 initial={{ opacity: 0, y: 28 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: .08, duration: .7 }}>Клиенты записываются сами.<br /><span>Вы занимаетесь любимым делом.</span></motion.h1>
          <motion.p initial={{ opacity: 0, y: 25 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: .16 }}>Zapisi Pro показывает клиентам свободное время, принимает записи и собирает расписание прямо в Telegram — без отдельного приложения.</motion.p>
          <motion.div className="hero-actions" initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: .24 }}>
            <Button size="lg" asChild><a href={TELEGRAM_URL} target="_blank" rel="noopener noreferrer">Попробовать бесплатно <ArrowRight size={18} /></a></Button>
          </motion.div>
          <motion.div className="hero-proof" initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: .4 }}><span><CheckCircle2 /> Запуск за 5 минут</span><span><CheckCircle2 /> Без банковской карты</span></motion.div>
        </div>
        <motion.div className="hero-visual" style={{ y: phoneY }} initial={{ opacity: 0, scale: .94, y: 30 }} animate={{ opacity: 1, scale: 1, y: 0 }} transition={{ delay: .18, duration: .8 }}>
          <div className="notification notification-a"><span><CalendarDays /></span><div><small>Новая запись</small><strong>Анна · Маникюр · 14:30</strong></div></div>
          <div className="notification notification-b"><span><Bell /></span><div><small>Напоминание отправлено</small><strong>Клиент получил уведомление</strong></div></div>
          <motion.div className="hero-mascot" animate={{ y: [0, -8, 0] }} transition={{ duration: 4.5, repeat: Infinity, ease: "easeInOut" }}><img src="/images/robot-pointing.png" alt="Робот-маскот Zapisi Pro показывает интерфейс приложения" /></motion.div>
          <Phone />
        </motion.div>
        <div className="scroll-hint"><MousePointer2 size={15} /> Листайте, чтобы увидеть магию</div>
      </section>

      <section className="audience"><span>Создано для тех, кто работает по записи</span><div>{["Маникюр", "Барбершоп", "Волосы", "Брови", "Косметология", "Массаж"].map(x => <b key={x}>{x}</b>)}</div></section>

      <section className="chaos-section">
        <SectionTitle eyebrow="Вместо хаоса" title="Записи не должны жить в пяти разных местах" text="Переписка, заметки и календарь помогают по отдельности. Zapisi Pro собирает весь процесс записи в одном месте." />
        <motion.div className="chaos-stage" {...reveal}>
          <div className="chat-stack"><div className="chat chat-1"><MessageCircle /> Завтра после шести есть окошко?</div><div className="chat chat-2">А можно перенести на пятницу?</div><div className="chat chat-3">Напомните, на сколько я записана 🙏</div></div>
          <div className="arrow-flow"><WandSparkles /><i /></div>
          <div className="result-card"><div className="result-top"><span><Check /></span><div><small>Запись подтверждена</small><strong>Мария</strong></div></div><div className="result-row"><span>Услуга</span><b>Маникюр + покрытие</b></div><div className="result-row"><span>Когда</span><b>Пятница, 18:30</b></div><div className="result-row"><span>Стоимость</span><b>2 400 ₽</b></div></div>
        </motion.div>
      </section>

      <section className="profile-showcase" id="profile">
        <motion.img className="profile-mascot-side" src="/images/robot-peeking.png" alt="Робот-маскот Zapisi Pro приветствует пользователя" initial={{ opacity: 0, x: 80 }} whileInView={{ opacity: 1, x: 0 }} viewport={{ once: true }} transition={{ duration: .8 }} />
        <div className="profile-showcase-copy">
          <SectionTitle eyebrow="Ваш публичный профиль" title="Страница мастера, которой хочется поделиться" text="Фото, портфолио, услуги и запись — в одном красивом профиле. Клиент сразу видит вашу работу и выбирает подходящую услугу." />
          <div className="profile-points"><span><CheckCircle2 /> Выглядит профессионально</span><span><CheckCircle2 /> Открывается внутри Telegram</span><span><CheckCircle2 /> Готов к записи клиентов</span></div>
          <Button size="lg" asChild><a href={TELEGRAM_URL} target="_blank" rel="noopener noreferrer">Создать свой профиль <ArrowRight size={18} /></a></Button>
        </div>
        <motion.div className="public-profile" {...reveal}>
          <div className="profile-telegram"><span>×</span><div><i><BriefcaseBusiness /></i> Zapisi.Pro</div><b>•••</b></div>
          <div className="public-head"><button>‹</button><div><strong>Профиль мастера</strong><small>Zapisi.Pro</small></div><button>•••</button></div>
          <div className="public-scroll">
            <div className="master-card-live">
              <img src="/images/master-sonya.png" alt="София, мастер по причёскам" />
              <div><strong>София</strong><span>@sofia.hair</span><a href="tel:+79994447878"><PhoneCall /> +7 999 444-78-78</a></div>
              <span className="live-badge">Принимаю записи</span>
            </div>
            <div className="about-live"><strong>О мастере</strong><p>Создаю причёски и укладки, которые подчёркивают вашу индивидуальность. Работаю с любовью к каждой детали.</p></div>
            <div className="portfolio-live"><strong>Портфолио</strong><div><span /><span /><span /></div></div>
            <div className="services-live"><strong>Услуги</strong>
              <article><span><BriefcaseBusiness /></span><div><b>Вечерняя укладка</b><small>от 2 500 ₽ · 1 ч 30 мин</small><em><MapPin /> Студия на Патриарших</em></div><ChevronRight /></article>
              <article><span><BriefcaseBusiness /></span><div><b>Свадебная причёска</b><small>от 5 000 ₽ · 2 часа</small><em><MapPin /> С выездом или в студии</em></div><ChevronRight /></article>
            </div>
          </div>
          <a className="book-live" href={TELEGRAM_URL} target="_blank" rel="noopener noreferrer">Записаться</a>
        </motion.div>
      </section>

      <section className="how-section" id="how">
        <SectionTitle eyebrow="Начните за несколько минут" title="Три шага — и ваша запись работает" text="Без внедрения, обучения сотрудников и отдельного приложения." />
        <div className="how-grid">
          <motion.article className="how-card setup-card" {...reveal}><span className="step-number">01</span><div className="setup-ui"><div><strong>Ваш профиль почти готов</strong><span>75%</span></div><i><b /></i>{['Добавьте фотографию','Создайте услуги','Настройте расписание'].map((x,i)=><p key={x}><span className={i<2?'done':''}>{i<2?<Check />:3}</span>{x}<ChevronRight /></p>)}</div><h3>Настройте профиль</h3><p>Мы подскажем, что заполнить. Технические знания не нужны.</p></motion.article>
          <motion.article className="how-card share-card" {...reveal} transition={{delay:.1}}><span className="step-number">02</span><div className="share-ui"><div className="profile-chip"><div className="avatar">А</div><div><strong>Анна</strong><small>Онлайн-запись</small></div></div><div className="link-chip">zapisi.pro/anna <span><Send /></span></div><div className="share-bubbles"><span><Send /></span><span><MessageCircle /></span><span><Instagram /></span></div></div><h3>Поделитесь ссылкой</h3><p>Отправьте её клиентам или добавьте в профиль и соцсети.</p></motion.article>
          <motion.article className="how-card booking-card" {...reveal} transition={{delay:.2}}><span className="step-number">03</span><div className="booking-ui"><div className="success-ring"><Check /></div><small>Новая запись</small><strong>Маникюр + покрытие</strong><p>13 августа · 14:30</p><span>Запись добавлена в расписание</span></div><h3>Получайте записи</h3><p>Клиент выбирает время, а вы получаете готовую запись.</p></motion.article>
        </div>
      </section>

      <section className="features-section" id="features">
        <SectionTitle eyebrow="Всё необходимое" title="Рабочий день — одним взглядом" text="Не перегруженная CRM, а понятные инструменты, которыми хочется пользоваться каждый день." />
        <div className="feature-grid">
          <motion.article className="feature feature-calendar" {...reveal}><div className="feature-copy"><span><CalendarDays /></span><h3>Расписание без путаницы</h3><p>Записи, свободные окна и личное время собраны в одном понятном календаре.</p></div><div className="week-ui"><div className="week-head"><span>Август 2026</span><div>‹ &nbsp; ›</div></div><div className="week-days">{['Пн 12','Вт 13','Ср 14','Чт 15','Пт 16'].map((d,i)=><b className={i===1?'active':''} key={d}>{d}</b>)}</div><div className="week-line"><time>10:00</time><div className="event blue">Мария · Маникюр</div></div><div className="week-line"><time>12:00</time><div className="event gray">Личное время</div></div><div className="week-line"><time>14:30</time><div className="event cyan">София · Дизайн</div></div></div></motion.article>
          <motion.article className="feature" {...reveal}><div className="feature-copy"><span><UsersRound /></span><h3>Клиенты всегда под рукой</h3><p>История записей и важная информация без поиска по чатам.</p></div><div className="client-list">{[['М','Мария','12 записей'],['Е','Елена','8 записей'],['С','София','5 записей']].map((c,i)=><div key={c[1]}><span className={`client-av c${i}`}>{c[0]}</span><p><b>{c[1]}</b><small>{c[2]} · была недавно</small></p><ChevronRight /></div>)}</div></motion.article>
          <motion.article className="feature" {...reveal}><div className="feature-copy"><span><Bell /></span><h3>Напоминания без ручной работы</h3><p>Клиент получает важную информацию о записи прямо в Telegram.</p></div><div className="reminder"><div className="reminder-head"><span><Send /></span><div><b>Zapisi Pro</b><small>сейчас</small></div></div><p>Мария, напоминаем о вашей записи завтра в 14:30</p><div>Маникюр + покрытие <b>2 400 ₽</b></div></div></motion.article>
          <motion.article className="feature" {...reveal}><div className="feature-copy"><span><BarChart3 /></span><h3>Только понятные цифры</h3><p>Следите за записями и доходом без сложных отчётов.</p></div><div className="stats-ui"><div><small>Доход за месяц</small><strong>84 200 ₽</strong><span>+12% к июлю</span></div><div className="bars">{[34,51,42,68,58,82,74,91,80,100,89,96].map((h,i)=><i key={i} style={{height:`${h}%`}} />)}</div></div></motion.article>
        </div>
      </section>

      <section className="comparison-section">
        <SectionTitle eyebrow="Меньше рутины" title="Не ещё один инструмент. Вместо нескольких." text="Путь клиента становится короче, а ваш день — спокойнее." />
        <div className="comparison-grid">
          <motion.div className="old-way" {...reveal}><div className="comparison-label">Как обычно</div>{['Ответить на сообщение','Найти свободное окно','Отправить прайс','Дождаться ответа','Внести запись в календарь','Напомнить клиенту'].map((x,i)=><div key={x}><span>{i+1}</span>{x}</div>)}<p>6 ручных действий на одну запись</p></motion.div>
          <motion.div className="new-way" {...reveal} transition={{delay:.12}}><div className="comparison-label">С Zapisi Pro</div><div className="one-link"><span><Send /></span><small>Ваша ссылка</small><strong>zapisi.pro/anna</strong></div><ArrowRight className="big-arrow" /><div className="ready"><span><Check /></span><div><small>Готово</small><strong>Запись в расписании</strong></div></div><p>Одна ссылка — остальное происходит само</p></motion.div>
        </div>
      </section>

      <section className="pricing-section" id="pricing">
        <SectionTitle eyebrow="Простая стоимость" title="Меньше цены одной записи" text="Все возможности доступны сразу. Выберите только удобный период оплаты." />
        <div className="pricing-grid">
          {[{name:'1 месяц',price:'349 ₽',note:'Оплата 349 ₽ за 1 месяц'},{name:'3 месяца',price:'290 ₽',note:'Оплата 870 ₽ за 3 месяца',badge:'Популярный'},{name:'1 год',price:'247 ₽',note:'Оплата 2 964 ₽ за 1 год',badge:'Выгодно'}].map((p,i)=><motion.article className={`price-card ${i===1?'featured':''}`} key={p.name} {...reveal} transition={{delay:i*.08}}>{p.badge&&<span className="price-badge">{p.badge}</span>}<small>{p.name}</small><h3>{p.price}</h3><b className="per-month">в месяц</b><p>{p.note}</p><ul><li><Check /> Онлайн-запись клиентов</li><li><Check /> Расписание и услуги</li><li><Check /> Профиль мастера</li><li><Check /> Telegram-напоминания</li></ul><Button variant={i===1?'primary':'secondary'} asChild><a href={TELEGRAM_URL} target="_blank" rel="noopener noreferrer">Попробовать бесплатно</a></Button></motion.article>)}
        </div>
        <p className="pricing-note"><CheckCircle2 /> Без банковской карты для старта</p>
      </section>

      <section className="faq-section" id="faq">
        <SectionTitle eyebrow="Вопросы и ответы" title="Всё, что важно перед стартом" />
        <Accordion type="single" collapsible className="faq-list">
          {[
            ['Нужно ли клиенту устанавливать приложение?','Нет. Запись открывается внутри Telegram — отдельное приложение скачивать не потребуется.'],
            ['Нужно ли разбираться в CRM?','Нет. Zapisi Pro по шагам поможет настроить профиль, услуги и расписание. Интерфейс рассчитан на обычного пользователя.'],
            ['Как клиент получит ссылку на запись?','Вы сможете отправить её в сообщении, добавить в Telegram-канал, социальные сети или описание профиля.'],
            ['Можно ли закрыть время для личных дел?','Да. Нужный интервал можно сделать недоступным, и клиенты больше не увидят его среди свободных окон.'],
            ['Что происходит после записи клиента?','Запись появляется в вашем расписании, а клиент получает подтверждение внутри Telegram.'],
          ].map(([q,a],i)=><AccordionItem value={`item-${i}`} key={q}><AccordionTrigger>{q}</AccordionTrigger><AccordionContent>{a}</AccordionContent></AccordionItem>)}
        </Accordion>
      </section>

      <section className="final-cta">
        <div className="cta-glow" />
        <motion.div {...reveal}><span className="eyebrow">Можно начать сегодня</span><h2>Пусть следующая запись<br />придёт сама</h2><p>Настройте Zapisi Pro за несколько минут и отправьте клиентам первую ссылку.</p><Button size="lg" asChild><a href={TELEGRAM_URL} target="_blank" rel="noopener noreferrer">Попробовать бесплатно <Send size={18} /></a></Button><small>Откроется прямо в Telegram</small></motion.div>
        <div className="cta-visual mascot-cta"><div className="cta-orbit"><Sparkles /></div><img src="/images/robot-beach.png" alt="Робот-маскот Zapisi Pro работает с ноутбуком на пляже" /><div className="mascot-note"><strong>Zapisi Pro работает</strong><span>пока вы отдыхаете</span></div></div>
      </section>

      <footer><Logo /><p>Онлайн-запись для мастеров внутри Telegram.</p><div><a href="#features">Возможности</a><a href="#pricing">Тарифы</a><a href="#faq">Поддержка</a><a href="/privacy">Конфиденциальность</a><a href="/consent">Согласие</a></div><span>© 2026 Zapisi Pro</span></footer>
      <a className="mobile-sticky" href={TELEGRAM_URL} target="_blank" rel="noopener noreferrer">Попробовать бесплатно <ArrowRight size={17} /></a>
    </main>
  )
}
