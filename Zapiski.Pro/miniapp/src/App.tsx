import { useEffect, useState } from "react"
import { Routes, Route, Link, useParams } from "react-router-dom"
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

type AdminStats = {
  users: number
  masters: number
  bookings: number
  payments: number
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/admin" element={<AdminPage />} />
      <Route path="/admin/masters" element={<MastersPage />} />
      <Route path="/admin/users" element={<UsersPage />} />
     <Route path="/master/:key" element={<MasterPanelStub />} />
      <Route path="/user/:telegramId" element={<UserPanelStub />} />
    </Routes>
  )
}
function MasterPanelStub() {
  const { key } = useParams()
  const [master, setMaster] = useState<Master | null>(null)
  const [loading, setLoading] = useState(true)
  const [denied, setDenied] = useState(false)

  useEffect(() => {
    fetch(`${API_URL}/api/master/${key}`)
      .then(async (res) => {
        if (!res.ok) {
          setDenied(true)
          return
        }

        const data = await res.json()
        setMaster(data)
      })
      .catch(() => setDenied(true))
      .finally(() => setLoading(false))
  }, [key])

  if (loading) {
    return (
      <div className="app">
        <section className="stubScreen">
          <div className="stubIcon">⏳</div>
          <h2>Загрузка...</h2>
        </section>
      </div>
    )
  }

  if (denied || !master) {
    return (
      <div className="app">
        <section className="stubScreen">
          <div className="stubIcon">⛔</div>
          <h2>Доступ запрещён</h2>
          <p>Мастер с таким ключом не найден</p>
        </section>
      </div>
    )
  }

  return (
    <div className="app">
      <header className="adminHeader">
        <h1>Панель мастера</h1>
        <p>@{master.username || "unknown"}</p>
      </header>

      <section className="stubScreen">
        <div className="stubIcon">💼</div>
        <h2>Раздел в разработке</h2>
        <p>
          Здесь будет личный кабинет мастера: записи, услуги,
          расписание, клиенты и аналитика.
        </p>

        <div className="modalInfo">
          <span>Ключ: {master.key}</span>
          <span>ID: {master.telegramId}</span>
        </div>
      </section>

      <BottomNav />
    </div>
  )
}

function UserPanelStub() {
  const { telegramId } = useParams()

  return (
    <div className="app">
      <header className="adminHeader">
        <h1>Панель клиента</h1>
        <p>Telegram ID: {telegramId}</p>
      </header>

      <section className="stubScreen">
        <div className="stubIcon">🚧</div>
        <h2>Раздел в разработке</h2>
        <p>
          Здесь будет кабинет клиента: мои записи, история,
          уведомления и профиль.
        </p>
      </section>

      <BottomNav />
    </div>
  )
}
function HomePage() {
  return (
    <div className="app">
      <header className="top">
        <h1>Zapisi.Pro</h1>
        <p>mini app</p>
      </header>

      <section className="hero">
        <div>
          <h2>Привет!</h2>
          <p>Zapisi.Pro помогает записывать клиентов без лишних забот</p>

          <Link to="/admin">
            <button>🛠 Админ панель ›</button>
          </Link>
        </div>

        <div className="robot">🤖</div>
      </section>

      <section className="grid">
        <Card icon="📅" title="Мои записи" text="Все записи клиентов в одном месте" />
        <Card icon="👥" title="Клиенты" text="Управляйте базой клиентов" />
        <Card icon="💼" title="Услуги" text="Настройте услуги и цены" />
        <Card icon="🗓️" title="Расписание" text="Управляйте временем" />
      </section>

      <BottomNav />
    </div>
  )
}

function AdminPage() {
  const [stats, setStats] = useState<AdminStats | null>(null)
console.log(
  "TG ID:",
  window.Telegram?.WebApp?.initDataUnsafe?.user?.id
)
  useEffect(() => {
  fetch(`${API_URL}/api/admin/stats`, {
    headers: {
      "X-Telegram-Id": String(
        window.Telegram?.WebApp?.initDataUnsafe?.user?.id ?? ""
      )
    }
  })
    .then((res) => res.json())
    .then((data) => setStats(data))
    .catch((err) => console.error("Ошибка загрузки stats:", err))
}, [])

  return (
    <div className="app">
      <Link to="/" className="backLink">⬅️ Назад</Link>

      <header className="adminHeader">
        <h1>Админ панель</h1>
        <p>Zapisi.Pro dashboard</p>
      </header>
      <p>
        TG ID: {
          window.Telegram?.WebApp?.initDataUnsafe?.user?.id
        }
      </p>
      <section className="statsGrid">
        <AdminStat title="Пользователи" value={stats?.users ?? "..."} icon="👥" />
        <AdminStat title="Мастера" value={stats?.masters ?? "..."} icon="💼" />
        <AdminStat title="Записи" value={stats?.bookings ?? "..."} icon="📅" />
        <AdminStat title="Оплаты" value={stats?.payments ?? "..."} icon="💳" />
      </section>

      <section className="adminCard">
        <h2>Разделы</h2>

        <Link to="/admin/masters" className="adminButtonLink">
          <AdminButton title="Мастера" text="Добавление, удаление и управление" />
        </Link>

        <Link to="/admin/users" className="adminButtonLink">
          <AdminButton title="Пользователи" text="База пользователей" />
        </Link>

        <AdminButton title="Рассылка" text="Отправить сообщение" />
        <AdminButton title="Аналитика" text="Графики и статистика" />
      </section>

      <BottomNav />
    </div>
  )
}

function MastersPage() {
  const [masters, setMasters] = useState<Master[]>([])
  const [deleteCandidate, setDeleteCandidate] = useState<Master | null>(null)
  const [showAddForm, setShowAddForm] = useState(false)
  const [telegramId, setTelegramId] = useState("")
  const [masterKey, setMasterKey] = useState("")
  const [message, setMessage] = useState("")

  function loadMasters() {
  fetch(`${API_URL}/api/admin/masters`, {
    headers: {
      "X-Telegram-Id": String(
        window.Telegram?.WebApp?.initDataUnsafe?.user?.id ?? ""
      )
    }
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
      "X-Telegram-Id": String(
        window.Telegram?.WebApp?.initDataUnsafe?.user?.id ?? ""
      )
    },
    body: JSON.stringify({
      telegramId: Number(telegramId),
      key: masterKey,
    }),
  })
    .then(async (res) => {
      const data = await res.json()

      if (!res.ok) {
        setMessage(data.message || "Ошибка создания мастера")
        return
      }

      setMessage("✅ Мастер добавлен")
      setTelegramId("")
      setMasterKey("")
      setShowAddForm(false)
      loadMasters()
    })
    .catch(() => setMessage("Ошибка соединения с сервером"))
}
function deleteMaster(id: number) {
  fetch(`${API_URL}/api/admin/masters/${id}`, {
    method: "DELETE",
    headers: {
      "X-Telegram-Id": String(
        window.Telegram?.WebApp?.initDataUnsafe?.user?.id ?? ""
      )
    }
  })
    .then(async (res) => {
      const data = await res.json()

      if (!res.ok) {
        setMessage(data.message || "Ошибка удаления мастера")
        return
      }

      setMessage("🗑 Мастер удалён")
      setDeleteCandidate(null)
      loadMasters()
    })
    .catch(() => setMessage("Ошибка соединения с сервером"))
}

  return (
    <div className="app">
      <Link to="/admin" className="backLink">⬅️ Назад</Link>

      <header className="adminHeader">
        <h1>Мастера</h1>
        <p>Список всех мастеров Zapisi.Pro</p>
      </header>

      <section className="adminCard">
        <button
          className="primaryButton"
          onClick={() => setShowAddForm(!showAddForm)}
        >
          {showAddForm ? "✖️ Закрыть" : "➕ Добавить мастера"}
        </button>

        {showAddForm && (
          <div className="addForm">
            <input
              className="adminInput"
              placeholder="Telegram ID пользователя"
              value={telegramId}
              onChange={(e) => setTelegramId(e.target.value)}
            />

            <input
              className="adminInput"
              placeholder="Ключ мастера"
              value={masterKey}
              onChange={(e) => setMasterKey(e.target.value)}
            />

            <button className="primaryButton" onClick={createMaster}>
              ✅ Создать мастера
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

              <button
                className="deleteButton"
                onClick={() => setDeleteCandidate(master)}
              >
                🗑
              </button>
            </div>
          ))
        )}
      </section>

        {deleteCandidate && (
  <div className="modalOverlay">
    <div className="modalCard">
      <div className="modalIcon">🗑</div>

      <h2>Удалить мастера?</h2>

      <p>
        Вы точно уверены, что хотите удалить мастера
        <br />
        <b>@{deleteCandidate.username || "unknown"}</b>?
      </p>

      <div className="modalInfo">
        <span>Ключ: {deleteCandidate.key}</span>
        <span>ID: {deleteCandidate.telegramId}</span>
      </div>

      <div className="modalActions">
        <button
          className="cancelButton"
          onClick={() => setDeleteCandidate(null)}
        >
          Отмена
        </button>

        <button
          className="dangerButton"
          onClick={() => deleteMaster(deleteCandidate.id)}
        >
          Да, удалить
        </button>
      </div>
    </div>
  </div>
)}
      <BottomNav />
    </div>
  )
}

function UsersPage() {
  const [users, setUsers] = useState<User[]>([])

 useEffect(() => {
  fetch(`${API_URL}/api/admin/users`, {
    headers: {
      "X-Telegram-Id": String(
        window.Telegram?.WebApp?.initDataUnsafe?.user?.id ?? ""
      )
    }
  })
    .then((res) => res.json())
    .then((data) => setUsers(data))
    .catch((err) => console.error("Ошибка загрузки пользователей:", err))
}, [])

  return (
    <div className="app">
      <Link to="/admin" className="backLink">⬅️ Назад</Link>

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

              <button className="smallAction">›</button>
            </div>
          ))
        )}
      </section>

      <BottomNav />
    </div>
  )
}

function BottomNav() {
  return (
    <nav className="bottomNav">
      <span>🏠<br />Главная</span>
      <span>📅<br />Записи</span>
      <button>＋</button>
      <span>👥<br />Клиенты</span>
      <span>👤<br />Профиль</span>
    </nav>
  )
}

function Card({ icon, title, text }: { icon: string; title: string; text: string }) {
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

function AdminButton({ title, text }: { title: string; text: string }) {
  return (
    <button className="adminButton">
      <div>
        <h3>{title}</h3>
        <p>{text}</p>
      </div>
      <span>›</span>
    </button>
  )
}

export default App