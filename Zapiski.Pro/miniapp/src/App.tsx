import { useEffect, useState } from "react"
import { Link, Route, Routes, useParams } from "react-router-dom"
import { API_URL } from "./config"
import "./App.css"

declare global {
  interface Window {
    Telegram?: any
  }
}

type User = {
  id: number
  telegramId: number
  username: string
  bookingsCount: number
}

type Master = {
  id: number
  key: string
  telegramId: number
  username: string
}

type MasterClient = {
  id: number
  telegramId: number
  username: string
  bookingsCount: number
  lastBookingAt: string | null
  lastStatus: string
}

type MasterStats = {
  clients: number
  activeBookings: number
  services: number
}

type AdminStats = {
  users: number
  masters: number
  bookings: number
  payments: number
}

const telegramId = () =>
  String(window.Telegram?.WebApp?.initDataUnsafe?.user?.id ?? "")

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/admin" element={<AdminPage />} />
      <Route path="/admin/masters" element={<MastersPage />} />
      <Route path="/admin/users" element={<UsersPage />} />
      <Route path="/admin/profile" element={<ComingSoon title="Профиль" subtitle="Раздел администратора" nav="admin" />} />

      <Route path="/master/:key" element={<MasterHomePage />} />
      <Route path="/master/:key/bookings" element={<MasterComingSoon title="Записи" />} />
      <Route path="/master/:key/services" element={<MasterComingSoon title="Услуги" />} />
      <Route path="/master/:key/schedule" element={<MasterComingSoon title="Расписание" />} />
      <Route path="/master/:key/clients" element={<MasterClientsPage />} />
      <Route path="/master/:key/profile" element={<MasterComingSoon title="Профиль" />} />
      <Route path="/master/:key/public-profile" element={<PublicProfileStub />} />

      <Route path="/user/:telegramId" element={<ComingSoon title="Кабинет клиента" subtitle="Скоро здесь появится личный кабинет клиента" />} />
    </Routes>
  )
}

function HomePage() {
  return (
    <main className="app">
      <header className="top">
        <h1>Zapisi.Pro</h1>
        <p>mini app</p>
      </header>

      <section className="hero">
        <div>
          <h2>Привет!</h2>
          <p>Zapisi.Pro помогает записывать клиентов без лишних забот</p>

          <Link to="/admin">
            <button>Админ панель ›</button>
          </Link>
        </div>

        <div className="robot">🤖</div>
      </section>

      <section className="grid">
        <Link to="/admin" className="cardLink">
          <Card icon="📊" title="Панель" text="Основная сводка и быстрые действия" />
        </Link>
        <Link to="/admin/masters" className="cardLink">
          <Card icon="💼" title="Мастера" text="Добавление и удаление мастеров" />
        </Link>
        <Link to="/admin/users" className="cardLink">
          <Card icon="👥" title="Пользователи" text="Список пользователей приложения" />
        </Link>
        <Link to="/admin/profile" className="cardLink">
          <Card icon="⚙️" title="Настройки" text="Служебные настройки кабинета" />
        </Link>
      </section>

      <AdminBottomNav />
    </main>
  )
}

function AdminPage() {
  const [stats, setStats] = useState<AdminStats | null>(null)

  useEffect(() => {
    fetch(`${API_URL}/api/admin/stats`, {
      headers: { "X-Telegram-Id": telegramId() },
    })
      .then((res) => res.json())
      .then((data) => setStats(data))
      .catch((err) => console.error("Ошибка загрузки stats:", err))
  }, [])

  return (
    <main className="app">
      <header className="top">
        <h1>Zapisi.Pro</h1>
        <p>админ панель</p>
      </header>

      <section className="adminHeader">
        <h1>Основная</h1>
        <p>Сводка по приложению и быстрые разделы</p>
      </section>

      <section className="statsGrid">
        <AdminStat title="Пользователи" value={stats?.users ?? "..."} icon="👥" />
        <AdminStat title="Мастера" value={stats?.masters ?? "..."} icon="💼" />
        <AdminStat title="Записи" value={stats?.bookings ?? "..."} icon="📅" />
        <AdminStat title="Оплаты" value={stats?.payments ?? "..."} icon="💳" />
      </section>

      <section className="grid">
        <Link to="/admin/masters" className="cardLink">
          <Card icon="💼" title="Мастера" text="Управляйте мастерами и доступом" />
        </Link>
        <Link to="/admin/users" className="cardLink">
          <Card icon="👥" title="Пользователи" text="Смотрите базу пользователей" />
        </Link>
      </section>

      <AdminBottomNav />
    </main>
  )
}

function MastersPage() {
  const [masters, setMasters] = useState<Master[]>([])
  const [deleteCandidate, setDeleteCandidate] = useState<Master | null>(null)
  const [showAddForm, setShowAddForm] = useState(false)
  const [telegramIdValue, setTelegramIdValue] = useState("")
  const [masterKey, setMasterKey] = useState("")
  const [message, setMessage] = useState("")

  function loadMasters() {
    fetch(`${API_URL}/api/admin/masters`, {
      headers: { "X-Telegram-Id": telegramId() },
    })
      .then((res) => res.json())
      .then((data) => setMasters(data))
      .catch((err) => console.error("Ошибка загрузки мастеров:", err))
  }

  useEffect(() => {
    loadMasters()
  }, [])

  function createMaster() {
    setMessage("")

    fetch(`${API_URL}/api/admin/masters`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": telegramId(),
      },
      body: JSON.stringify({
        telegramId: Number(telegramIdValue),
        key: masterKey,
      }),
    })
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok) {
          setMessage(data.message || "Ошибка создания мастера")
          return
        }

        setMessage("Мастер добавлен")
        setTelegramIdValue("")
        setMasterKey("")
        setShowAddForm(false)
        loadMasters()
      })
      .catch(() => setMessage("Ошибка соединения с сервером"))
  }

  function deleteMaster(id: number) {
    fetch(`${API_URL}/api/admin/masters/${id}`, {
      method: "DELETE",
      headers: { "X-Telegram-Id": telegramId() },
    })
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok) {
          setMessage(data.message || "Ошибка удаления мастера")
          return
        }

        setMessage("Мастер удален")
        setDeleteCandidate(null)
        loadMasters()
      })
      .catch(() => setMessage("Ошибка соединения с сервером"))
  }

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Мастера</h1>
        <p>Добавление, удаление и просмотр мастеров</p>
      </header>

      <section className="adminCard">
        <button className="primaryButton" onClick={() => setShowAddForm(!showAddForm)}>
          {showAddForm ? "Закрыть" : "Добавить мастера"}
        </button>

        {showAddForm && (
          <div className="addForm">
            <input
              className="adminInput"
              placeholder="Telegram ID пользователя"
              value={telegramIdValue}
              onChange={(e) => setTelegramIdValue(e.target.value)}
            />

            <input
              className="adminInput"
              placeholder="Ключ мастера"
              value={masterKey}
              onChange={(e) => setMasterKey(e.target.value)}
            />

            <button className="primaryButton" onClick={createMaster}>
              Создать мастера
            </button>
          </div>
        )}

        {message && <p className="formMessage">{message}</p>}
      </section>

      <section className="mastersList">
        {masters.length === 0 ? (
          <div className="emptyCard">Мастера не найдены</div>
        ) : (
          masters.map((master) => (
            <div className="masterCard" key={master.id}>
              <div className="masterAvatar">💼</div>

              <div className="masterInfo">
                <h3>@{master.username || "unknown"}</h3>
                <p>Ключ: {master.key}</p>
                <span>ID: {master.telegramId}</span>
              </div>

              <button className="deleteButton" onClick={() => setDeleteCandidate(master)}>
                ×
              </button>
            </div>
          ))
        )}
      </section>

      {deleteCandidate && (
        <div className="modalOverlay">
          <div className="modalCard">
            <div className="modalIcon">×</div>
            <h2>Удалить мастера?</h2>
            <p>
              Вы точно хотите удалить <b>@{deleteCandidate.username || "unknown"}</b>?
            </p>

            <div className="modalInfo">
              <span>Ключ: {deleteCandidate.key}</span>
              <span>ID: {deleteCandidate.telegramId}</span>
            </div>

            <div className="modalActions">
              <button className="cancelButton" onClick={() => setDeleteCandidate(null)}>
                Отмена
              </button>
              <button className="dangerButton" onClick={() => deleteMaster(deleteCandidate.id)}>
                Удалить
              </button>
            </div>
          </div>
        </div>
      )}

      <AdminBottomNav />
    </main>
  )
}

function UsersPage() {
  const [users, setUsers] = useState<User[]>([])

  useEffect(() => {
    fetch(`${API_URL}/api/admin/users`, {
      headers: { "X-Telegram-Id": telegramId() },
    })
      .then((res) => res.json())
      .then((data) => setUsers(data))
      .catch((err) => console.error("Ошибка загрузки пользователей:", err))
  }, [])

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Пользователи</h1>
        <p>Все пользователи Zapisi.Pro</p>
      </header>

      <section className="mastersList">
        {users.length === 0 ? (
          <div className="emptyCard">Пользователи не найдены</div>
        ) : (
          users.map((user) => (
            <div className="masterCard" key={user.id}>
              <div className="masterAvatar">👤</div>

              <div className="masterInfo">
                <h3>@{user.username || "unknown"}</h3>
                <p>ID Telegram: {user.telegramId}</p>
                <span>Записей: {user.bookingsCount}</span>
              </div>
            </div>
          ))
        )}
      </section>

      <AdminBottomNav />
    </main>
  )
}

function MasterHomePage() {
  const { key } = useParams()
  const [master, setMaster] = useState<Master | null>(null)
  const [stats, setStats] = useState<MasterStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [denied, setDenied] = useState(false)

  useEffect(() => {
    fetch(`${API_URL}/api/master/${key}`)
      .then(async (res) => {
        if (!res.ok) {
          setDenied(true)
          return
        }

        setMaster(await res.json())
      })
      .catch(() => setDenied(true))
      .finally(() => setLoading(false))
  }, [key])

  useEffect(() => {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/stats`)
      .then((res) => res.json())
      .then((data) => setStats(data))
      .catch(() => setStats(null))
  }, [key])

  if (loading) {
    return <ComingSoon title="Загрузка..." subtitle="Получаем данные мастера" />
  }

  if (denied || !master) {
    return <ComingSoon title="Доступ закрыт" subtitle="Мастер с таким ключом не найден" />
  }

  return (
    <main className="app">
      <header className="top">
        <h1>Zapisi.Pro</h1>
        <p>кабинет мастера</p>
      </header>

      <section className="hero">
        <div>
          <h2>Привет!</h2>
          <p>@{master.username || "master"}, это ваша мастер-панель</p>
          <Link to={`/master/${master.key}/bookings`}>
            <button>Мои записи ›</button>
          </Link>
        </div>

        <div className="robot">💼</div>
      </section>

      <section className="grid">
        <Link to={`/master/${master.key}/bookings`} className="cardLink">
          <Card icon="📅" title="Записи" text="Скоро здесь появятся записи" />
        </Link>
        <Link to={`/master/${master.key}/clients`} className="cardLink">
          <Card icon="👥" title="Клиенты" text="База клиентов мастера" />
        </Link>
        <Link to={`/master/${master.key}/services`} className="cardLink">
          <Card icon="💼" title="Услуги" text="Настройка услуг и цен" />
        </Link>
        <Link to={`/master/${master.key}/schedule`} className="cardLink">
          <Card icon="🗓️" title="Расписание" text="Управление временем" />
        </Link>
        <Link to={`/master/${master.key}/public-profile`} className="cardLink">
          <Card icon="🌐" title="Профиль" text="Так страницу будут видеть клиенты" />
        </Link>
      </section>

      <section className="adminCard statsBlock">
        <h2>Статистика</h2>
        <div className="statsGrid compactStats">
          <AdminStat title="Клиенты" value={stats?.clients ?? "..."} icon="👥" />
          <AdminStat title="Активные записи" value={stats?.activeBookings ?? "..."} icon="📅" />
          <AdminStat title="Услуги" value={stats?.services ?? "..."} icon="💼" />
        </div>
      </section>

      <MasterBottomNav masterKey={master.key} />
    </main>
  )
}

function PublicProfileStub() {
  const { key } = useParams()

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Публичный профиль</h1>
        <p>Так эту страницу будут видеть клиенты</p>
      </header>

      <section className="stubScreen">
        <div className="stubIcon">🌐</div>
        <h2>Скоро здесь будет профиль мастера</h2>
        <p>
          Тут появятся фото, описание, услуги, контакты и кнопка записи для клиентов.
        </p>
        <Link to={`/master/${key}`} className="inlineButton">
          Назад в кабинет
        </Link>
      </section>

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function MasterClientsPage() {
  const { key } = useParams()
  const [clients, setClients] = useState<MasterClient[]>([])
  const [loading, setLoading] = useState(true)
  const [query, setQuery] = useState("")

  useEffect(() => {
    fetch(`${API_URL}/api/master/${key}/clients`)
      .then((res) => res.json())
      .then((data) => setClients(Array.isArray(data) ? data : []))
      .catch((err) => console.error("Ошибка загрузки клиентов мастера:", err))
      .finally(() => setLoading(false))
  }, [key])

  const normalizedQuery = query.trim().toLowerCase()
  const filteredClients = clients.filter((client) => {
    const username = client.username?.toLowerCase() ?? ""
    const telegramIdText = String(client.telegramId)
    return username.includes(normalizedQuery) || telegramIdText.includes(normalizedQuery)
  })

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Клиенты</h1>
        <p>Клиенты, которые записывались к вам</p>
      </header>

      <section className="adminCard">
        <input
          className="adminInput searchInput"
          placeholder="Поиск по имени или Telegram ID"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
      </section>

      <section className="mastersList">
        {loading ? (
          <div className="emptyCard">Загружаем клиентов...</div>
        ) : filteredClients.length === 0 ? (
          <div className="emptyCard">Клиенты не найдены</div>
        ) : (
          filteredClients.map((client) => (
            <div className="masterCard clientCard" key={client.id}>
              <div className="masterAvatar">👤</div>

              <div className="masterInfo">
                <div className="clientTitleRow">
                  <h3>@{client.username || "unknown"}</h3>
                  <StatusBadge status={client.lastStatus} />
                </div>
                <p>ID Telegram: {client.telegramId}</p>
                <span>
                  Записей: {client.bookingsCount}
                  {client.lastBookingAt ? ` · последний раз ${client.lastBookingAt}` : " · еще не записывался"}
                </span>
              </div>
            </div>
          ))
        )}
      </section>

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function MasterComingSoon({ title }: { title: string }) {
  const { key } = useParams()

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>{title}</h1>
        <p>Раздел мастера</p>
      </header>

      <section className="stubScreen">
        <div className="stubIcon">🚧</div>
        <h2>Скоро тут появятся записи</h2>
        <p>Этот раздел уже подключен в меню, а рабочая логика появится следующим шагом.</p>
        <Link to={`/master/${key}`} className="inlineButton">
          На главную
        </Link>
      </section>

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function ComingSoon({
  title,
  subtitle,
  nav,
}: {
  title: string
  subtitle: string
  nav?: "admin"
}) {
  return (
    <main className="app">
      <section className="stubScreen">
        <div className="stubIcon">🚧</div>
        <h2>{title}</h2>
        <p>{subtitle}</p>
      </section>
      {nav === "admin" && <AdminBottomNav />}
    </main>
  )
}

function StatusBadge({ status }: { status: string | null }) {
  const normalizedStatus = status ?? "inactive"
  const labelByStatus: Record<string, string> = {
    inactive: "Не активен",
    pending: "Ждет подтверждения",
    confirmed: "Активен",
    waiting_payment: "Ждет предоплату",
    waiting_payment_confirm: "Предоплата",
    cancelled: "Отменен",
    completed: "Завершен",
  }

  return (
    <span className={`statusBadge status-${normalizedStatus}`}>
      {labelByStatus[normalizedStatus] ?? normalizedStatus}
    </span>
  )
}

function AdminBottomNav() {
  return (
    <nav className="bottomNav">
      <Link to="/admin" className="bottomNavItem">
        <span>🏠</span>
        <small>Основная</small>
      </Link>
      <Link to="/admin/masters" className="bottomNavItem">
        <span>💼</span>
        <small>Мастера</small>
      </Link>
      <Link to="/admin" className="bottomNavMain">
        +
      </Link>
      <Link to="/admin/users" className="bottomNavItem">
        <span>👥</span>
        <small>Пользователи</small>
      </Link>
      <Link to="/admin/profile" className="bottomNavItem">
        <span>👤</span>
        <small>Профиль</small>
      </Link>
    </nav>
  )
}

function MasterBottomNav({ masterKey }: { masterKey: string }) {
  return (
    <nav className="bottomNav">
      <Link to={`/master/${masterKey}`} className="bottomNavItem">
        <span>🏠</span>
        <small>Главная</small>
      </Link>
      <Link to={`/master/${masterKey}/bookings`} className="bottomNavItem">
        <span>📅</span>
        <small>Записи</small>
      </Link>
      <Link to={`/master/${masterKey}`} className="bottomNavMain">
        +
      </Link>
      <Link to={`/master/${masterKey}/clients`} className="bottomNavItem">
        <span>👥</span>
        <small>Клиенты</small>
      </Link>
      <Link to={`/master/${masterKey}/profile`} className="bottomNavItem">
        <span>👤</span>
        <small>Профиль</small>
      </Link>
    </nav>
  )
}

function Card({
  icon,
  title,
  text,
}: {
  icon: string
  title: string
  text: string
}) {
  return (
    <div className="card">
      <div className="icon">{icon}</div>
      <h3>{title}</h3>
      <p>{text}</p>
      <span className="arrow">›</span>
    </div>
  )
}

function AdminStat({
  title,
  value,
  icon,
}: {
  title: string
  value: string | number
  icon: string
}) {
  return (
    <div className="statCard">
      <span>{icon}</span>
      <p>{title}</p>
      <h2>{value}</h2>
    </div>
  )
}

export default App
