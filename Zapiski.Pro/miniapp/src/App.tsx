import { type ReactNode, useEffect, useState } from "react"
import { Link, Route, Routes, useParams } from "react-router-dom"
import {
  Bot,
  BriefcaseBusiness,
  CalendarCheck,
  CalendarDays,
  Clock,
  ChevronRight,
  Construction,
  CreditCard,
  Globe,
  Home,
  LayoutDashboard,
  Plus,
  BadgePercent,
  Banknote,
  Pencil,
  Trash2,
  Settings,
  ShieldCheck,
  User,
  Users,
  X,
} from "lucide-react"
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

type MasterServiceItem = {
  id: number
  name: string
  price: number
  duration: number
  prepaymentPercent: number
  prepaymentAmount: number
}

type AdminStats = {
  users: number
  masters: number
  bookings: number
  payments: number
}

type UserDashboard = {
  profile: {
    id: number
    telegramId: number
    username: string
  }
  roles: {
    isAdmin: boolean
    isMaster: boolean
    masterKey: string | null
  }
  bookings: UserBooking[]
  masters: UserMaster[]
}

type UserBooking = {
  id: number
  serviceName: string
  masterKey: string
  masterUsername: string
  dateTime: string
  status: string
}

type UserMaster = {
  id: number
  key: string
  username: string
  bookingsCount: number
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
      <Route path="/master/:key/services" element={<MasterServicesPage />} />
      <Route path="/master/:key/schedule" element={<MasterComingSoon title="Расписание" />} />
      <Route path="/master/:key/clients" element={<MasterClientsPage />} />
      <Route path="/master/:key/profile" element={<MasterComingSoon title="Профиль" />} />
      <Route path="/master/:key/public-profile" element={<PublicProfileStub />} />

      <Route path="/user/:telegramId" element={<UserHomePage />} />
      <Route path="/user/:telegramId/bookings" element={<UserBookingsPage />} />
      <Route path="/user/:telegramId/masters" element={<UserMastersPage />} />
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

        <div className="heroIcon">
          <Bot size={56} strokeWidth={2.1} />
        </div>
      </section>

      <section className="grid">
        <Link to="/admin" className="cardLink">
          <Card icon={<LayoutDashboard />} title="Панель" text="Основная сводка и быстрые действия" />
        </Link>
        <Link to="/admin/masters" className="cardLink">
          <Card icon={<BriefcaseBusiness />} title="Мастера" text="Добавление и удаление мастеров" />
        </Link>
        <Link to="/admin/users" className="cardLink">
          <Card icon={<Users />} title="Пользователи" text="Список пользователей приложения" />
        </Link>
        <Link to="/admin/profile" className="cardLink">
          <Card icon={<Settings />} title="Настройки" text="Служебные настройки кабинета" />
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

      <ClientCabinetBanner />

      <section className="statsGrid">
        <AdminStat title="Пользователи" value={stats?.users ?? "..."} icon={<Users />} />
        <AdminStat title="Мастера" value={stats?.masters ?? "..."} icon={<BriefcaseBusiness />} />
        <AdminStat title="Записи" value={stats?.bookings ?? "..."} icon={<CalendarDays />} />
        <AdminStat title="Оплаты" value={stats?.payments ?? "..."} icon={<CreditCard />} />
      </section>

      <section className="grid">
        <Link to="/admin/masters" className="cardLink">
          <Card icon={<BriefcaseBusiness />} title="Мастера" text="Управляйте мастерами и доступом" />
        </Link>
        <Link to="/admin/users" className="cardLink">
          <Card icon={<Users />} title="Пользователи" text="Смотрите базу пользователей" />
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
              <div className="masterAvatar">
                <BriefcaseBusiness size={23} strokeWidth={2.3} />
              </div>

              <div className="masterInfo">
                <h3>@{master.username || "unknown"}</h3>
                <p>Ключ: {master.key}</p>
                <span>ID: {master.telegramId}</span>
              </div>

              <button className="deleteButton" onClick={() => setDeleteCandidate(master)}>
                <X size={20} strokeWidth={2.5} />
              </button>
            </div>
          ))
        )}
      </section>

      {deleteCandidate && (
        <div className="modalOverlay">
          <div className="modalCard">
            <div className="modalIcon">
              <X size={28} strokeWidth={2.5} />
            </div>
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
              <div className="masterAvatar">
                <User size={23} strokeWidth={2.3} />
              </div>

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

        <div className="heroIcon">
          <BriefcaseBusiness size={56} strokeWidth={2.1} />
        </div>
      </section>

      <ClientCabinetBanner telegramId={master.telegramId} />

      <section className="grid">
        <Link to={`/master/${master.key}/bookings`} className="cardLink">
          <Card icon={<CalendarCheck />} title="Записи" text="Скоро здесь появятся записи" />
        </Link>
        <Link to={`/master/${master.key}/clients`} className="cardLink">
          <Card icon={<Users />} title="Клиенты" text="База клиентов мастера" />
        </Link>
        <Link to={`/master/${master.key}/services`} className="cardLink">
          <Card icon={<BriefcaseBusiness />} title="Услуги" text="Настройка услуг и цен" />
        </Link>
        <Link to={`/master/${master.key}/schedule`} className="cardLink">
          <Card icon={<CalendarDays />} title="Расписание" text="Управление временем" />
        </Link>
        <Link to={`/master/${master.key}/public-profile`} className="cardLink">
          <Card icon={<Globe />} title="Профиль" text="Так страницу будут видеть клиенты" />
        </Link>
      </section>

      <section className="adminCard statsBlock">
        <h2>Статистика</h2>
        <div className="masterStatsList">
          <MasterStatRow
            to={`/master/${master.key}/clients`}
            icon={<Users size={26} strokeWidth={2.4} />}
            title="Клиенты"
            value={stats?.clients ?? "..."}
          />
          <MasterStatRow
            to={`/master/${master.key}/bookings`}
            icon={<CalendarCheck size={26} strokeWidth={2.4} />}
            title="Активные записи"
            value={stats?.activeBookings ?? "..."}
          />
          <MasterStatRow
            to={`/master/${master.key}/services`}
            icon={<BriefcaseBusiness size={26} strokeWidth={2.4} />}
            title="Услуги"
            value={stats?.services ?? "..."}
          />
        </div>
      </section>

      <MasterBottomNav masterKey={master.key} />
    </main>
  )
}

function PublicProfileStub() {
  const currentTelegramId = telegramId()
  const clientCabinetUrl = currentTelegramId ? `/user/${currentTelegramId}` : "/"

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Публичный профиль</h1>
        <p>Так эту страницу будут видеть клиенты</p>
      </header>

      <section className="stubScreen">
        <div className="stubIcon">
          <Globe size={38} strokeWidth={2.2} />
        </div>
        <h2>Скоро здесь будет профиль мастера</h2>
        <p>
          Тут появятся фото, описание, услуги, контакты и кнопка записи для клиентов.
        </p>
        <Link to={clientCabinetUrl} className="inlineButton">
          Назад
        </Link>
      </section>

      {currentTelegramId && <PublicProfileBottomNav telegramId={currentTelegramId} />}
    </main>
  )
}

function useUserDashboard() {
  const { telegramId: routeTelegramId } = useParams()
  const [dashboard, setDashboard] = useState<UserDashboard | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState("")

  useEffect(() => {
    const currentTelegramId = telegramId()

    if (!currentTelegramId) {
      setError("Откройте кабинет из Telegram")
      setLoading(false)
      return
    }

    if (currentTelegramId !== routeTelegramId) {
      setError("Доступ закрыт: это не ваш профиль")
      setLoading(false)
      return
    }

    fetch(`${API_URL}/api/user/${routeTelegramId}/dashboard`, {
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const text = await res.text()
        const data = text ? JSON.parse(text) : {}

        if (!res.ok) {
          setError(data.message || "Доступ закрыт")
          return
        }

        setDashboard(data)
      })
      .catch(() => setError("Ошибка соединения с сервером"))
      .finally(() => setLoading(false))
  }, [routeTelegramId])

  return { dashboard, loading, error, routeTelegramId }
}

function UserHomePage() {
  const { dashboard, loading, error } = useUserDashboard()

  if (loading) {
    return <ComingSoon title="Загрузка..." subtitle="Открываем кабинет клиента" />
  }

  if (error || !dashboard) {
    return <ComingSoon title="Доступ закрыт" subtitle={error || "Кабинет недоступен"} />
  }

  return (
    <main className="app">
      <header className="top">
        <h1>Zapisi.Pro</h1>
        <p>кабинет клиента</p>
      </header>

      <section className="hero">
        <div>
          <h2>Привет!</h2>
          <p>@{dashboard.profile.username || "client"}, здесь ваши записи и мастера</p>
        </div>

        <div className="heroIcon">
          <ShieldCheck size={56} strokeWidth={2.1} />
        </div>
      </section>

      <RoleSwitchBanners dashboard={dashboard} />

      <section className="grid">
        <Link to={`/user/${dashboard.profile.telegramId}/bookings`} className="cardLink">
          <Card icon={<CalendarCheck />} title="Мои записи" text={`${dashboard.bookings.length} записей`} />
        </Link>
        <Link to={`/user/${dashboard.profile.telegramId}/masters`} className="cardLink">
          <Card icon={<BriefcaseBusiness />} title="Мастера" text="К кому вы уже записывались" />
        </Link>
      </section>

      <UserBookingsSection
        title="Последние записи"
        bookings={dashboard.bookings.slice(0, 3)}
        emptyText="Записей пока нет"
      />

      <UserMastersSection
        title="Последние мастера"
        masters={dashboard.masters.slice(0, 3)}
        emptyText="Вы пока не записывались к мастерам"
      />

      <UserBottomNav telegramId={dashboard.profile.telegramId} />
    </main>
  )
}

function UserBookingsPage() {
  const { dashboard, loading, error } = useUserDashboard()

  if (loading) {
    return <ComingSoon title="Загрузка..." subtitle="Получаем ваши записи" />
  }

  if (error || !dashboard) {
    return <ComingSoon title="Доступ закрыт" subtitle={error || "Записи недоступны"} />
  }

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Мои записи</h1>
        <p>Все ваши записи у мастеров</p>
      </header>

      <UserBookingsSection
        title="Все записи"
        bookings={dashboard.bookings}
        emptyText="Записей пока нет"
      />

      <UserBottomNav telegramId={dashboard.profile.telegramId} />
    </main>
  )
}

function UserMastersPage() {
  const { dashboard, loading, error } = useUserDashboard()

  if (loading) {
    return <ComingSoon title="Загрузка..." subtitle="Получаем ваших мастеров" />
  }

  if (error || !dashboard) {
    return <ComingSoon title="Доступ закрыт" subtitle={error || "Мастера недоступны"} />
  }

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Мои мастера</h1>
        <p>Мастера, к которым вы уже записывались</p>
      </header>

      <UserMastersSection
        title="Все мастера"
        masters={dashboard.masters}
        emptyText="Вы пока не записывались к мастерам"
      />

      <UserBottomNav telegramId={dashboard.profile.telegramId} />
    </main>
  )
}

function UserBookingsSection({
  title,
  bookings,
  emptyText,
}: {
  title: string
  bookings: UserBooking[]
  emptyText: string
}) {
  return (
    <section className="adminCard clientSection">
      <h2>{title}</h2>
      <div className="clientList">
        {bookings.length === 0 ? (
          <div className="emptyLine">{emptyText}</div>
        ) : (
          bookings.map((booking) => (
            <div className="clientListItem" key={booking.id}>
              <span className="masterAvatar">
                <CalendarCheck size={23} strokeWidth={2.3} />
              </span>
              <div className="masterInfo">
                <div className="clientTitleRow">
                  <h3>{booking.serviceName || "Услуга"}</h3>
                  <StatusBadge status={booking.status} />
                </div>
                <p>@{booking.masterUsername || booking.masterKey}</p>
                <span>{booking.dateTime}</span>
              </div>
            </div>
          ))
        )}
      </div>
    </section>
  )
}

function UserMastersSection({
  title,
  masters,
  emptyText,
}: {
  title: string
  masters: UserMaster[]
  emptyText: string
}) {
  return (
    <section className="adminCard clientSection">
      <h2>{title}</h2>
      <div className="clientList">
        {masters.length === 0 ? (
          <div className="emptyLine">{emptyText}</div>
        ) : (
          masters.map((master) => (
            <Link to={`/master/${master.key}/public-profile`} className="clientListItem clientListLink" key={master.id}>
              <span className="masterAvatar">
                <BriefcaseBusiness size={23} strokeWidth={2.3} />
              </span>
              <div className="masterInfo">
                <h3>@{master.username || "master"}</h3>
                <p>Записей: {master.bookingsCount}</p>
                <span>Открыть профиль мастера</span>
              </div>
              <ChevronRight className="masterStatArrow" size={22} strokeWidth={2.4} />
            </Link>
          ))
        )}
      </div>
    </section>
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
              <div className="masterAvatar">
                <User size={23} strokeWidth={2.3} />
              </div>

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

function MasterServicesPage() {
  const { key } = useParams()
  const [services, setServices] = useState<MasterServiceItem[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [name, setName] = useState("")
  const [price, setPrice] = useState("")
  const [duration, setDuration] = useState("")
  const [prepaymentPercent, setPrepaymentPercent] = useState("0")
  const [message, setMessage] = useState("")
  const [loadError, setLoadError] = useState("")
  const [editingService, setEditingService] = useState<MasterServiceItem | null>(null)

  function loadServices() {
    setLoading(true)
    setLoadError("")

    fetch(`${API_URL}/api/master/${key}/services`)
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok) {
          setLoadError(data.message || "Не удалось загрузить услуги")
          setServices([])
          return
        }

        if (!Array.isArray(data)) {
          setLoadError("Сервер вернул неверный формат услуг")
          setServices([])
          return
        }

        setServices(data)
      })
      .catch((err) => {
        console.error("Ошибка загрузки услуг мастера:", err)
        setServices([])
        setLoadError("Ошибка соединения с сервером")
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    loadServices()
  }, [key])

  function resetServiceForm() {
    setName("")
    setPrice("")
    setDuration("")
    setPrepaymentPercent("0")
    setEditingService(null)
  }

  function startEditService(service: MasterServiceItem) {
    setEditingService(service)
    setName(service.name)
    setPrice(String(service.price))
    setDuration(String(service.duration))
    setPrepaymentPercent(String(service.prepaymentPercent))
    setShowForm(true)
    setMessage("")
  }

  function closeServiceForm() {
    setShowForm(false)
    resetServiceForm()
    setMessage("")
  }

  function saveService() {
    setMessage("")

    const priceValue = Number(price)
    const durationValue = Number(duration)
    const prepaymentValue = Number(prepaymentPercent)

    if (!name.trim()) {
      setMessage("Введите название услуги")
      return
    }

    if (!Number.isInteger(priceValue) || priceValue <= 0) {
      setMessage("Цена должна быть больше 0")
      return
    }

    if (!Number.isInteger(durationValue) || durationValue <= 0) {
      setMessage("Длительность должна быть больше 0")
      return
    }

    if (!Number.isInteger(prepaymentValue) || prepaymentValue < 0 || prepaymentValue > 100) {
      setMessage("Предоплата должна быть от 0 до 100%")
      return
    }

    const serviceUrl = editingService
      ? `${API_URL}/api/master/${key}/services/${editingService.id}`
      : `${API_URL}/api/master/${key}/services`

    fetch(serviceUrl, {
      method: editingService ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: name.trim(),
        price: priceValue,
        duration: durationValue,
        prepaymentPercent: prepaymentValue,
      }),
    })
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok) {
          setMessage(data.message || "Ошибка добавления услуги")
          return
        }

        setMessage(editingService ? "Услуга обновлена" : "Услуга добавлена")
        resetServiceForm()
        setShowForm(false)
        loadServices()
      })
      .catch(() => setMessage("Ошибка соединения с сервером"))
  }

  function deleteService(serviceId: number) {
    setMessage("")

    fetch(`${API_URL}/api/master/${key}/services/${serviceId}`, {
      method: "DELETE",
    })
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok) {
          setMessage(data.message || "Ошибка удаления услуги")
          return
        }

        setMessage("Услуга удалена")
        loadServices()
      })
      .catch(() => setMessage("Ошибка соединения с сервером"))
  }

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Услуги</h1>
        <p>Добавляйте услуги, цены, время и предоплату</p>
      </header>

      <section className="adminCard serviceFormCard">
        <button className="primaryButton" onClick={() => (showForm ? closeServiceForm() : setShowForm(true))}>
          {showForm ? "Закрыть" : "Добавить услугу"}
        </button>

        {showForm && (
          <div className="addForm">
            {editingService && (
              <div className="editingNotice">
                <Pencil size={18} strokeWidth={2.4} />
                <span>Редактируете услугу</span>
              </div>
            )}

            <input
              className="adminInput"
              placeholder="Название услуги"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />

            <div className="formGrid">
              <input
                className="adminInput"
                inputMode="numeric"
                placeholder="Цена"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
              />
              <input
                className="adminInput"
                inputMode="numeric"
                placeholder="Минуты"
                value={duration}
                onChange={(e) => setDuration(e.target.value)}
              />
            </div>

            <label className="fieldLabel" htmlFor="prepaymentPercent">
              Предоплата: {prepaymentPercent || 0}%
            </label>
            <input
              id="prepaymentPercent"
              className="rangeInput"
              type="range"
              min="0"
              max="100"
              step="5"
              value={prepaymentPercent}
              onChange={(e) => setPrepaymentPercent(e.target.value)}
            />

            <div className="formActions">
              {editingService && (
                <button className="cancelButton" onClick={closeServiceForm}>
                  Отмена
                </button>
              )}
              <button className="primaryButton" onClick={saveService}>
                {editingService ? "Сохранить" : "Создать услугу"}
              </button>
            </div>
          </div>
        )}

        {message && <p className="formMessage">{message}</p>}
      </section>

      <section className="servicesList">
        {loading ? (
          <div className="emptyCard">Загружаем услуги...</div>
        ) : loadError ? (
          <div className="emptyCard">{loadError}</div>
        ) : services.length === 0 ? (
          <div className="emptyCard">Услуги пока не добавлены</div>
        ) : (
          services.map((service) => (
            <article className="serviceCard" key={service.id} onClick={() => startEditService(service)}>
              <div className="serviceCardTop">
                <span className="masterAvatar">
                  <BriefcaseBusiness size={23} strokeWidth={2.3} />
                </span>
                <div className="serviceMain">
                  <h3>{service.name}</h3>
                  <p>Нажмите, чтобы редактировать</p>
                </div>
                <button
                  className="deleteButton"
                  onClick={(event) => {
                    event.stopPropagation()
                    deleteService(service.id)
                  }}
                >
                  <Trash2 size={19} strokeWidth={2.4} />
                </button>
              </div>

              <div className="serviceMetaGrid">
                <ServiceMeta icon={<Banknote />} label="Цена" value={`${service.price}₽`} />
                <ServiceMeta icon={<Clock />} label="Время" value={`${service.duration} мин`} />
                <ServiceMeta
                  icon={<BadgePercent />}
                  label="Предоплата"
                  value={
                    service.prepaymentPercent > 0
                      ? `${service.prepaymentAmount}₽ (${service.prepaymentPercent}%)`
                      : "Без предоплаты"
                  }
                />
              </div>
            </article>
          ))
        )}
      </section>

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function ServiceMeta({
  icon,
  label,
  value,
}: {
  icon: ReactNode
  label: string
  value: string
}) {
  return (
    <div className="serviceMeta">
      <span>{icon}</span>
      <small>{label}</small>
      <strong>{value}</strong>
    </div>
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
        <div className="stubIcon">
          <Construction size={38} strokeWidth={2.2} />
        </div>
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
        <div className="stubIcon">
          <Construction size={38} strokeWidth={2.2} />
        </div>
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

function MasterStatRow({
  to,
  icon,
  title,
  value,
}: {
  to: string
  icon: ReactNode
  title: string
  value: string | number
}) {
  return (
    <Link to={to} className="masterStatRow">
      <span className="masterStatIcon">{icon}</span>
      <span className="masterStatText">
        <small>{title}</small>
        <strong>{value}</strong>
      </span>
      <span className="masterStatTrend">↗</span>
      <ChevronRight className="masterStatArrow" size={24} strokeWidth={2.4} />
    </Link>
  )
}

function AdminBottomNav() {
  return (
    <nav className="bottomNav">
      <Link to="/admin" className="bottomNavItem">
        <span><Home size={20} strokeWidth={2.2} /></span>
        <small>Основная</small>
      </Link>
      <Link to="/admin/masters" className="bottomNavItem">
        <span><BriefcaseBusiness size={20} strokeWidth={2.2} /></span>
        <small>Мастера</small>
      </Link>
      <Link to="/admin" className="bottomNavMain">
        <Plus size={30} strokeWidth={2.4} />
      </Link>
      <Link to="/admin/users" className="bottomNavItem">
        <span><Users size={20} strokeWidth={2.2} /></span>
        <small>Пользователи</small>
      </Link>
      <Link to="/admin/profile" className="bottomNavItem">
        <span><User size={20} strokeWidth={2.2} /></span>
        <small>Профиль</small>
      </Link>
    </nav>
  )
}

function MasterBottomNav({ masterKey }: { masterKey: string }) {
  return (
    <nav className="bottomNav">
      <Link to={`/master/${masterKey}`} className="bottomNavItem">
        <span><Home size={20} strokeWidth={2.2} /></span>
        <small>Главная</small>
      </Link>
      <Link to={`/master/${masterKey}/bookings`} className="bottomNavItem">
        <span><CalendarCheck size={20} strokeWidth={2.2} /></span>
        <small>Записи</small>
      </Link>
      <Link to={`/master/${masterKey}`} className="bottomNavMain">
        <Plus size={30} strokeWidth={2.4} />
      </Link>
      <Link to={`/master/${masterKey}/clients`} className="bottomNavItem">
        <span><Users size={20} strokeWidth={2.2} /></span>
        <small>Клиенты</small>
      </Link>
      <Link to={`/master/${masterKey}/profile`} className="bottomNavItem">
        <span><User size={20} strokeWidth={2.2} /></span>
        <small>Профиль</small>
      </Link>
    </nav>
  )
}

function UserBottomNav({ telegramId }: { telegramId: number }) {
  return (
    <nav className="bottomNav">
      <Link to={`/user/${telegramId}`} className="bottomNavItem">
        <span><Home size={20} strokeWidth={2.2} /></span>
        <small>Главная</small>
      </Link>
      <Link to={`/user/${telegramId}/bookings`} className="bottomNavItem">
        <span><CalendarCheck size={20} strokeWidth={2.2} /></span>
        <small>Записи</small>
      </Link>
      <Link to={`/user/${telegramId}`} className="bottomNavMain">
        <Plus size={30} strokeWidth={2.4} />
      </Link>
      <Link to={`/user/${telegramId}/masters`} className="bottomNavItem">
        <span><BriefcaseBusiness size={20} strokeWidth={2.2} /></span>
        <small>Мастера</small>
      </Link>
      <Link to={`/user/${telegramId}`} className="bottomNavItem">
        <span><User size={20} strokeWidth={2.2} /></span>
        <small>Профиль</small>
      </Link>
    </nav>
  )
}

function PublicProfileBottomNav({ telegramId }: { telegramId: string }) {
  return (
    <nav className="bottomNav">
      <Link to={`/user/${telegramId}`} className="bottomNavItem">
        <span><Home size={20} strokeWidth={2.2} /></span>
        <small>Главная</small>
      </Link>
      <Link to={`/user/${telegramId}/bookings`} className="bottomNavItem">
        <span><CalendarCheck size={20} strokeWidth={2.2} /></span>
        <small>Записи</small>
      </Link>
      <Link to={`/user/${telegramId}`} className="bottomNavMain">
        <Plus size={30} strokeWidth={2.4} />
      </Link>
      <Link to={`/user/${telegramId}/masters`} className="bottomNavItem">
        <span><BriefcaseBusiness size={20} strokeWidth={2.2} /></span>
        <small>Мастера</small>
      </Link>
      <Link to={`/user/${telegramId}`} className="bottomNavItem">
        <span><User size={20} strokeWidth={2.2} /></span>
        <small>Профиль</small>
      </Link>
    </nav>
  )
}

function RoleSwitchBanners({ dashboard }: { dashboard: UserDashboard }) {
  const hasRoles = dashboard.roles?.isAdmin || dashboard.roles?.isMaster

  if (!hasRoles) {
    return null
  }

  return (
    <section className="roleSwitchList">
      {dashboard.roles?.isMaster && dashboard.roles.masterKey && (
        <Link to={`/master/${dashboard.roles.masterKey}`} className="clientCabinetBanner">
          <span className="clientCabinetIcon">
            <BriefcaseBusiness size={22} strokeWidth={2.3} />
          </span>
          <span className="clientCabinetText">
            <strong>Мастер-панель</strong>
            <small>Услуги, клиенты и записи</small>
          </span>
          <ChevronRight size={22} strokeWidth={2.4} />
        </Link>
      )}

      {dashboard.roles?.isAdmin && (
        <Link to="/admin" className="clientCabinetBanner">
          <span className="clientCabinetIcon">
            <Settings size={22} strokeWidth={2.3} />
          </span>
          <span className="clientCabinetText">
            <strong>Админ-панель</strong>
            <small>Мастера, пользователи и настройки</small>
          </span>
          <ChevronRight size={22} strokeWidth={2.4} />
        </Link>
      )}
    </section>
  )
}

function ClientCabinetBanner({ telegramId: fallbackTelegramId }: { telegramId?: number }) {
  const currentTelegramId = fallbackTelegramId ? String(fallbackTelegramId) : telegramId()

  if (!currentTelegramId) {
    return null
  }

  return (
    <Link to={`/user/${currentTelegramId}`} className="clientCabinetBanner">
      <span className="clientCabinetIcon">
        <User size={22} strokeWidth={2.3} />
      </span>
      <span className="clientCabinetText">
        <strong>Клиентский кабинет</strong>
        <small>Ваши записи и мастера</small>
      </span>
      <ChevronRight size={22} strokeWidth={2.4} />
    </Link>
  )
}

function Card({
  icon,
  title,
  text,
}: {
  icon: ReactNode
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
  icon: ReactNode
}) {
  return (
    <div className="statCard">
      <span className="statIcon">{icon}</span>
      <p>{title}</p>
      <h2>{value}</h2>
    </div>
  )
}

export default App
