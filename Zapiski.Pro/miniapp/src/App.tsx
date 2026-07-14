import { type ChangeEvent, type PointerEvent, type ReactNode, useEffect, useRef, useState } from "react"
import { Link, Route, Routes, useNavigate, useParams, useSearchParams } from "react-router-dom"
import {
  Bot,
  BriefcaseBusiness,
  CalendarCheck,
  CalendarDays,
  Camera,
  Clock,
  ChevronRight,
  Construction,
  CreditCard,
  CircleHelp,
  FileText,
  BarChart3,
  Globe,
  Home,
  Info,
  LayoutDashboard,
  MapPin,
  Megaphone,
  Menu,
  Plus,
  BadgePercent,
  Banknote,
  ArrowLeft,
  ArrowRight,
  Image as ImageIcon,
  Maximize2,
  Pencil,
  Phone,
  Trash2,
  Settings,
  Shield,
  ShieldCheck,
  Send,
  Star,
  User,
  Users,
  X,
  XCircle,
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
  name: string
  description: string
  paymentDetails: string
  phoneNumber: string
  avatarUrl: string
  isFounder: boolean
  trialEndsAt: string | null
  subscriptionEndsAt: string | null
  subscriptionPlan: string
  hasAccess: boolean
  accessType: string
  daysLeft: number
}

const normalizeMaster = (master: any): Master => ({
  ...master,
  avatarUrl: master?.avatarUrl ?? master?.AvatarUrl ?? "",
  isFounder: master?.isFounder ?? master?.IsFounder ?? false,
  trialEndsAt: master?.trialEndsAt ?? master?.TrialEndsAt ?? null,
  subscriptionEndsAt: master?.subscriptionEndsAt ?? master?.SubscriptionEndsAt ?? null,
  subscriptionPlan: master?.subscriptionPlan ?? master?.SubscriptionPlan ?? "",
  hasAccess: master?.hasAccess ?? master?.HasAccess ?? false,
  accessType: master?.accessType ?? master?.AccessType ?? "expired",
  daysLeft: master?.daysLeft ?? master?.DaysLeft ?? 0,
})

type SubscriptionPlan = {
  code: string
  title: string
  months: number
  priceRub: number
}

type MasterSubscription = {
  hasAccess: boolean
  isFounder: boolean
  accessType: string
  trialEndsAt: string | null
  subscriptionEndsAt: string | null
  subscriptionPlan: string
  daysLeft: number
  availablePlans: SubscriptionPlan[]
}

type MasterOnboardingStep = {
  code: string
  title: string
  text: string
  url: string
  isDone: boolean
  isRequired: boolean
  severity: string
  warningText: string
}

type MasterOnboardingTip = {
  code: string
  title: string
  text: string
  url: string
  severity: string
}

type MasterOnboarding = {
  completedCount: number
  totalCount: number
  progressPercent: number
  isComplete: boolean
  nextTitle: string
  nextText: string
  nextUrl: string
  steps: MasterOnboardingStep[]
  tips: MasterOnboardingTip[]
}

type SubscriptionPaymentStatus = {
  success: boolean
  message: string
  paymentId: string
  paymentToken: string
  planCode: string
  months: number
  amountRub: number
  status: string
  confirmationUrl: string
  createdAt: string
  paidAt: string | null
}

const subscriptionPlanPrice = (code: string, months: number) => {
  if (code === "month" || months === 1) return 399
  if (code === "quarter" || months === 3) return 999
  if (code === "year" || months === 12) return 3360
  return 0
}

const normalizeSubscription = (subscription: any): MasterSubscription => ({
  hasAccess: subscription?.hasAccess ?? subscription?.HasAccess ?? false,
  isFounder: subscription?.isFounder ?? subscription?.IsFounder ?? false,
  accessType: subscription?.accessType ?? subscription?.AccessType ?? "expired",
  trialEndsAt: subscription?.trialEndsAt ?? subscription?.TrialEndsAt ?? null,
  subscriptionEndsAt: subscription?.subscriptionEndsAt ?? subscription?.SubscriptionEndsAt ?? null,
  subscriptionPlan: subscription?.subscriptionPlan ?? subscription?.SubscriptionPlan ?? "",
  daysLeft: subscription?.daysLeft ?? subscription?.DaysLeft ?? 0,
  availablePlans: (subscription?.availablePlans ?? subscription?.AvailablePlans ?? []).map((plan: any) => {
    const code = plan?.code ?? plan?.Code ?? ""
    const months = plan?.months ?? plan?.Months ?? 0
    const priceRub = plan?.priceRub ?? plan?.PriceRub ?? subscriptionPlanPrice(code, months)

    return {
      code,
      title: plan?.title ?? plan?.Title ?? "",
      months,
      priceRub,
    }
  }),
})

const normalizePaymentStatus = (payment: any): SubscriptionPaymentStatus => ({
  success: payment?.success ?? payment?.Success ?? false,
  message: payment?.message ?? payment?.Message ?? "",
  paymentId: payment?.paymentId ?? payment?.PaymentId ?? "",
  paymentToken: payment?.paymentToken ?? payment?.PaymentToken ?? "",
  planCode: payment?.planCode ?? payment?.PlanCode ?? "",
  months: payment?.months ?? payment?.Months ?? 0,
  amountRub: payment?.amountRub ?? payment?.AmountRub ?? 0,
  status: payment?.status ?? payment?.Status ?? "",
  confirmationUrl: payment?.confirmationUrl ?? payment?.ConfirmationUrl ?? "",
  createdAt: payment?.createdAt ?? payment?.CreatedAt ?? "",
  paidAt: payment?.paidAt ?? payment?.PaidAt ?? null,
})

const normalizeOnboarding = (onboarding: any): MasterOnboarding => ({
  completedCount: onboarding?.completedCount ?? onboarding?.CompletedCount ?? 0,
  totalCount: onboarding?.totalCount ?? onboarding?.TotalCount ?? 0,
  progressPercent: onboarding?.progressPercent ?? onboarding?.ProgressPercent ?? 0,
  isComplete: onboarding?.isComplete ?? onboarding?.IsComplete ?? false,
  nextTitle: onboarding?.nextTitle ?? onboarding?.NextTitle ?? "",
  nextText: onboarding?.nextText ?? onboarding?.NextText ?? "",
  nextUrl: onboarding?.nextUrl ?? onboarding?.NextUrl ?? "",
  steps: (onboarding?.steps ?? onboarding?.Steps ?? []).map((step: any) => ({
    code: step?.code ?? step?.Code ?? "",
    title: step?.title ?? step?.Title ?? "",
    text: step?.text ?? step?.Text ?? "",
    url: step?.url ?? step?.Url ?? "",
    isDone: step?.isDone ?? step?.IsDone ?? false,
    isRequired: step?.isRequired ?? step?.IsRequired ?? false,
    severity: step?.severity ?? step?.Severity ?? "normal",
    warningText: step?.warningText ?? step?.WarningText ?? "",
  })),
  tips: (onboarding?.tips ?? onboarding?.Tips ?? []).map((tip: any) => ({
    code: tip?.code ?? tip?.Code ?? "",
    title: tip?.title ?? tip?.Title ?? "",
    text: tip?.text ?? tip?.Text ?? "",
    url: tip?.url ?? tip?.Url ?? "",
    severity: tip?.severity ?? tip?.Severity ?? "info",
  })),
})

type MasterClient = {
  id: number
  telegramId: number
  username: string
  bookingsCount: number
  lastBookingAt: string | null
  lastStatus: string
}

type MasterAnalyticsDay = {
  date: string
  completedBookings: number
  completedRevenue: number
  potentialRevenue: number
}

type MasterAnalytics = {
  from: string
  to: string
  completedRevenue: number
  potentialRevenue: number
  completedBookings: number
  activeBookings: number
  clients: number
  days: MasterAnalyticsDay[]
}

type MasterBooking = {
  id: number
  clientTelegramId: number
  clientUsername: string
  serviceName: string
  address: string
  dateTime: string
  status: string
  price: number
  prepaymentPercent: number
  prepaymentAmount: number
  isManualBlock?: boolean
}

type MasterScheduleDay = {
  dayOfWeek: number
  dayName: string
  startTime: string
  endTime: string
  isActive: boolean
}

type MasterManualSlot = {
  id: number
  date: string
  startTime: string
  endTime: string
}

type MasterAddress = {
  id: number
  title: string
  address: string
}

type BookingSlot = {
  time: string
  isBusy: boolean
}

type PublicAvailableSlot = {
  serviceId: number
  serviceName: string
  date: string
  label: string
  time: string
}

type PortfolioPhoto = {
  id: number
  imageUrl: string
  sortOrder: number
}

const normalizePortfolioPhoto = (photo: any): PortfolioPhoto => ({
  id: photo?.id ?? photo?.Id ?? 0,
  imageUrl: photo?.imageUrl ?? photo?.ImageUrl ?? "",
  sortOrder: photo?.sortOrder ?? photo?.SortOrder ?? 0,
})

type BookingCreateResult = {
  success: boolean
  message: string
  bookingId: number
  status: string
  serviceName: string
  price: number
  prepaymentPercent: number
  prepaymentAmount: number
  paymentDetails: string
  address: string
}

type MasterServiceItem = {
  id: number
  name: string
  price: number
  isVariablePrice: boolean
  maxPrice: number | null
  duration: number
  prepaymentPercent: number
  prepaymentAmount: number
  addressId: number | null
  addressTitle: string
  address: string
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
    phoneNumber: string
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
  address: string
  masterKey: string
  masterUsername: string
  dateTime: string
  status: string
}

type UserMaster = {
  id: number
  key: string
  username: string
  avatarUrl: string
  bookingsCount: number
}

const normalizeUserMaster = (master: any): UserMaster => ({
  ...master,
  avatarUrl: master?.avatarUrl ?? master?.AvatarUrl ?? "",
})

type TopMenuItem = {
  to: string
  title: string
  subtitle?: string
  icon: ReactNode
  danger?: boolean
}

type InfoMode = "user" | "master"
type InfoTabId = "faq" | "rules" | "privacy" | "offer" | "statuses"

const telegramId = () =>
  String(window.Telegram?.WebApp?.initDataUnsafe?.user?.id ?? "")

const formatDateInput = (date: Date) => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, "0")
  const day = String(date.getDate()).padStart(2, "0")
  return `${year}-${month}-${day}`
}

const buildCalendarDays = (count: number) => {
  const weekdays = ["Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"]
  const months = ["янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек"]

  return Array.from({ length: count }, (_, index) => {
    const date = new Date()
    date.setDate(date.getDate() + index)

    return {
      value: formatDateInput(date),
      weekday: weekdays[date.getDay()],
      day: String(date.getDate()),
      month: months[date.getMonth()],
    }
  })
}

const isHistoryStatus = (status: string | null) =>
  status === "cancelled" || status === "completed"

const splitBookingDateTime = (dateTime: string | null) => {
  const [date = "Без даты", time = "--:--"] = (dateTime ?? "").split(" ")
  return { date, time }
}

const getWeekdayLabel = (dateText: string) => {
  const [day, month, year] = dateText.split(".").map(Number)
  const date = new Date(year, month - 1, day)
  const weekdays = ["Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"]
  return weekdays[date.getDay()] ?? ""
}

const compareBookingDates = (first: string, second: string) => {
  const [firstDay, firstMonth, firstYear] = first.split(".").map(Number)
  const [secondDay, secondMonth, secondYear] = second.split(".").map(Number)
  return new Date(firstYear, firstMonth - 1, firstDay).getTime() - new Date(secondYear, secondMonth - 1, secondDay).getTime()
}

const formatServicePrice = (service: Pick<MasterServiceItem, "price" | "isVariablePrice" | "maxPrice">) =>
  service.isVariablePrice && service.maxPrice && service.maxPrice > service.price
    ? `${service.price}₽ - ${service.maxPrice}₽`
    : `${service.price}₽`

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/admin" element={<AdminPage />} />
      <Route path="/admin/masters" element={<MastersPage />} />
      <Route path="/admin/users" element={<UsersPage />} />
      <Route path="/admin/profile" element={<ComingSoon title="Профиль" subtitle="Раздел администратора" nav="admin" />} />

      <Route path="/master/:key" element={<MasterHomePage />} />
      <Route path="/master/:key/onboarding" element={<MasterOnboardingPage />} />
      <Route path="/master/:key/bookings" element={<MasterBookingsPage />} />
      <Route path="/master/:key/block-time" element={<MasterTimeBlockPage />} />
      <Route path="/master/:key/analytics" element={<MasterAnalyticsPage />} />
      <Route path="/master/:key/services" element={<MasterServicesPage />} />
      <Route path="/master/:key/schedule" element={<MasterSchedulePage />} />
      <Route path="/master/:key/clients" element={<MasterClientsPage />} />
      <Route path="/master/:key/broadcast" element={<MasterBroadcastPage />} />
      <Route path="/master/:key/subscription" element={<MasterSubscriptionPage />} />
      <Route path="/master/:key/subscription/payment/:paymentToken" element={<MasterSubscriptionPaymentPage />} />
      <Route path="/master/:key/info" element={<InformationPage mode="master" />} />
      <Route path="/master/:key/profile" element={<MasterProfilePage />} />
      <Route path="/master/:key/public-profile" element={<PublicProfileStub />} />
      <Route path="/master/:key/public-services" element={<PublicServicesPage />} />
      <Route path="/master/:key/public-booking" element={<PublicBookingStub />} />

      <Route path="/user/:telegramId" element={<UserHomePage />} />
      <Route path="/user/:telegramId/bookings" element={<UserBookingsPage />} />
      <Route path="/user/:telegramId/masters" element={<UserMastersPage />} />
      <Route path="/user/:telegramId/info" element={<InformationPage mode="user" />} />
      <Route path="/user/:telegramId/become-master" element={<BecomeMasterPage />} />
      <Route path="/become-master" element={<BecomeMasterPage />} />
    </Routes>
  )
}

function AppTopBar({
  subtitle,
  username,
  homeUrl,
  infoUrl,
  menuItems,
}: {
  subtitle: string
  username?: string
  homeUrl: string
  infoUrl: string
  menuItems: TopMenuItem[]
}) {
  const [menuOpen, setMenuOpen] = useState(false)

  return (
    <>
      <header className="appTopBar">
        <Link to={homeUrl} className="appBrand">
          <span className="appBrandLogo">
            <BriefcaseBusiness size={25} strokeWidth={2.3} />
          </span>
          <span>
            <strong>Zapisi.Pro</strong>
            <small>{subtitle}</small>
          </span>
        </Link>

        <div className="appTopActions">
          <Link to={infoUrl} className="appTopIconButton" aria-label="Информация">
            <Info size={21} strokeWidth={2.4} />
          </Link>
          <button
            type="button"
            className="appTopIconButton appMenuButton"
            aria-label={menuOpen ? "Закрыть меню" : "Открыть меню"}
            onClick={() => setMenuOpen((value) => !value)}
          >
            {menuOpen ? <X size={25} strokeWidth={2.4} /> : <Menu size={26} strokeWidth={2.4} />}
          </button>
        </div>
      </header>

      {menuOpen && (
        <div className="topMenuOverlay">
          <div className="topMenuPanel">
            <div className="topMenuUser">
              <span className="topMenuAvatar">
                <User size={23} strokeWidth={2.3} />
              </span>
              <span>
                <strong>{username ? `@${username}` : "Пользователь"}</strong>
                <small>{subtitle}</small>
              </span>
            </div>

            <nav className="topMenuList">
              {menuItems.map((item) => (
                <Link
                  to={item.to}
                  className={`topMenuItem ${item.danger ? "danger" : ""}`}
                  key={item.to}
                  onClick={() => setMenuOpen(false)}
                >
                  <span>{item.icon}</span>
                  <div>
                    <strong>{item.title}</strong>
                    {item.subtitle && <small>{item.subtitle}</small>}
                  </div>
                  <ChevronRight size={18} strokeWidth={2.3} />
                </Link>
              ))}
            </nav>
          </div>
        </div>
      )}
    </>
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

function getUserTopMenuItems(userTelegramId: number): TopMenuItem[] {
  return [
    {
      to: `/user/${userTelegramId}`,
      title: "Главная",
      subtitle: "Кабинет клиента",
      icon: <Home size={21} strokeWidth={2.3} />,
    },
    {
      to: `/user/${userTelegramId}/bookings`,
      title: "Мои записи",
      subtitle: "Будущие и прошлые визиты",
      icon: <CalendarCheck size={21} strokeWidth={2.3} />,
    },
    {
      to: `/user/${userTelegramId}/masters`,
      title: "Мастера",
      subtitle: "К кому вы уже записывались",
      icon: <BriefcaseBusiness size={21} strokeWidth={2.3} />,
    },
    {
      to: `/user/${userTelegramId}/become-master`,
      title: "Стать мастером",
      subtitle: "Запустить свою онлайн-запись",
      icon: <BadgePercent size={21} strokeWidth={2.3} />,
    },
    {
      to: `/user/${userTelegramId}/info`,
      title: "Информация",
      subtitle: "FAQ, правила и документы",
      icon: <Info size={21} strokeWidth={2.3} />,
    },
  ]
}

function getMasterTopMenuItems(masterKey: string): TopMenuItem[] {
  return [
    {
      to: `/master/${masterKey}`,
      title: "Главная",
      subtitle: "Сводка мастер-панели",
      icon: <Home size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/onboarding`,
      title: "Настройка",
      subtitle: "Шаги для запуска профиля",
      icon: <LayoutDashboard size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/bookings`,
      title: "Записи",
      subtitle: "Заявки и визиты клиентов",
      icon: <CalendarCheck size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/clients`,
      title: "Клиенты",
      subtitle: "База и история посещений",
      icon: <Users size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/analytics`,
      title: "Статистика",
      subtitle: "Доход, записи и клиенты",
      icon: <BarChart3 size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/services`,
      title: "Услуги",
      subtitle: "Цены, длительность и адреса",
      icon: <BriefcaseBusiness size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/schedule`,
      title: "Расписание",
      subtitle: "Рабочие дни и свободные окна",
      icon: <CalendarDays size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/broadcast`,
      title: "Рассылка",
      subtitle: "Сообщения клиентам",
      icon: <Megaphone size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/subscription`,
      title: "Подписка",
      subtitle: "Тариф и доступ к панели",
      icon: <CreditCard size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/profile`,
      title: "Профиль",
      subtitle: "Фото, описание и портфолио",
      icon: <User size={21} strokeWidth={2.3} />,
    },
    {
      to: `/master/${masterKey}/info`,
      title: "Информация",
      subtitle: "FAQ, правила и документы",
      icon: <Info size={21} strokeWidth={2.3} />,
    },
  ]
}

function MastersPage() {
  const [masters, setMasters] = useState<Master[]>([])
  const [deleteCandidate, setDeleteCandidate] = useState<Master | null>(null)
  const [showAddForm, setShowAddForm] = useState(false)
  const [telegramIdValue, setTelegramIdValue] = useState("")
  const [masterKey, setMasterKey] = useState("")
  const [isFounder, setIsFounder] = useState(false)
  const [subscriptionMonths, setSubscriptionMonths] = useState("0")
  const [message, setMessage] = useState("")

  function loadMasters() {
    fetch(`${API_URL}/api/admin/masters`, {
      headers: { "X-Telegram-Id": telegramId() },
    })
      .then((res) => res.json())
      .then((data) => setMasters(Array.isArray(data) ? data.map(normalizeMaster) : []))
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
        isFounder,
        subscriptionMonths: Number(subscriptionMonths),
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
        setIsFounder(false)
        setSubscriptionMonths("0")
        setShowAddForm(false)
        loadMasters()
      })
      .catch(() => setMessage("Ошибка соединения с сервером"))
  }

  function grantSubscription(masterId: number, months: number, founder?: boolean) {
    setMessage("Обновляем доступ...")

    fetch(`${API_URL}/api/admin/masters/${masterId}/subscription`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": telegramId(),
      },
      body: JSON.stringify({
        isFounder: founder ?? false,
        changeFounderStatus: founder !== undefined,
        subscriptionMonths: months,
      }),
    })
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok || data?.success === false) {
          setMessage(data.message || "Ошибка обновления подписки")
          return
        }

        setMessage(data.message || "Доступ обновлён")
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

            <label className="adminCheckRow">
              <input
                type="checkbox"
                checked={isFounder}
                onChange={(event) => setIsFounder(event.target.checked)}
              />
              Первый мастер, доступ навсегда
            </label>

            {!isFounder && (
              <select
                className="adminInput"
                value={subscriptionMonths}
                onChange={(event) => setSubscriptionMonths(event.target.value)}
              >
                <option value="0">30 дней trial</option>
                <option value="1">Сразу подписка на 1 месяц</option>
                <option value="3">Сразу подписка на 3 месяца</option>
                <option value="12">Сразу подписка на год</option>
              </select>
            )}

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
                {master.avatarUrl ? (
                  <img src={master.avatarUrl} alt={master.username || "Мастер"} />
                ) : (
                  <BriefcaseBusiness size={23} strokeWidth={2.3} />
                )}
              </div>

              <div className="masterInfo">
                <h3>@{master.username || "unknown"}</h3>
                <p>Ключ: {master.key}</p>
                <span>ID: {master.telegramId}</span>
                <span className={`subscriptionBadge ${master.hasAccess ? "active" : "expired"}`}>
                  {master.accessType === "founder"
                    ? "Первый мастер"
                    : master.accessType === "trial"
                      ? `Trial: ${master.daysLeft} дн.`
                      : master.accessType === "paid"
                        ? `Подписка: ${master.daysLeft} дн.`
                        : "Доступ закончился"}
                </span>
                <div className="adminSubscriptionActions">
                  <button type="button" onClick={() => grantSubscription(master.id, 0, !master.isFounder)}>
                    {master.isFounder ? "Снять Founder" : "Founder"}
                  </button>
                  <button type="button" onClick={() => grantSubscription(master.id, 1)}>
                    +1 мес
                  </button>
                  <button type="button" onClick={() => grantSubscription(master.id, 3)}>
                    +3 мес
                  </button>
                  <button type="button" onClick={() => grantSubscription(master.id, 12)}>
                    +1 год
                  </button>
                </div>
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
  const [subscription, setSubscription] = useState<MasterSubscription | null>(null)
  const [onboarding, setOnboarding] = useState<MasterOnboarding | null>(null)
  const [loading, setLoading] = useState(true)
  const [denied, setDenied] = useState(false)
  const [copyMessage, setCopyMessage] = useState("")

  useEffect(() => {
    fetch(`${API_URL}/api/master/${key}`)
      .then(async (res) => {
        if (!res.ok) {
          setDenied(true)
          return
        }

        setMaster(normalizeMaster(await res.json()))
      })
      .catch(() => setDenied(true))
      .finally(() => setLoading(false))
  }, [key])

  useEffect(() => {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/subscription`)
      .then((res) => res.ok ? res.json() : null)
      .then((data) => setSubscription(data ? normalizeSubscription(data) : null))
      .catch(() => setSubscription(null))
  }, [key])

  useEffect(() => {
    if (!key || !master) return

    fetch(`${API_URL}/api/master/${key}/onboarding`, {
      headers: { "X-Telegram-Id": telegramId() || String(master.telegramId) },
    })
      .then((res) => res.ok ? res.json() : null)
      .then((data) => setOnboarding(data ? normalizeOnboarding(data) : null))
      .catch(() => setOnboarding(null))
  }, [key, master])

  if (loading) {
    return <ComingSoon title="Загрузка..." subtitle="Получаем данные мастера" />
  }

  if (denied || !master) {
    return <ComingSoon title="Доступ закрыт" subtitle="Мастер с таким ключом не найден" />
  }

  if (subscription && !subscription.hasAccess) {
    const lockedMenuItems = getMasterTopMenuItems(master.key)

    return (
      <main className="app">
        <AppTopBar
          subtitle="кабинет мастера"
          username={master.username}
          homeUrl={`/master/${master.key}`}
          infoUrl={`/master/${master.key}/info`}
          menuItems={lockedMenuItems}
        />

        <section className="subscriptionLockedCard">
          <div className="subscriptionLockedIcon">
            <CreditCard size={34} strokeWidth={2.4} />
          </div>
          <h2>Доступ закончился</h2>
          <p>Оформите подписку, чтобы снова управлять записями, расписанием, услугами, клиентами и рассылками.</p>
          <Link to={`/master/${master.key}/subscription`} className="primaryButton subscriptionLockedButton">
            Перейти к подписке
          </Link>
        </section>

        <MasterBottomNav masterKey={master.key} />
      </main>
    )
  }

  const clientBotLink = `https://t.me/ZapisiProBot?start=${master.key}`
  const shouldShowSubscriptionBanner = subscription && !subscription.isFounder
  const isSubscriptionCountdown = shouldShowSubscriptionBanner && subscription.hasAccess && subscription.daysLeft <= 3
  const subscriptionBannerTitle = subscription?.accessType === "paid"
    ? "Подписка активна"
    : subscription?.accessType === "trial"
      ? "Пробный период активен"
      : "Доступ закончился"
  const subscriptionBannerText = subscription?.hasAccess
    ? isSubscriptionCountdown
      ? `Осталось ${subscription.daysLeft} дн. Продлите доступ, чтобы панель не остановилась.`
      : `Осталось ${subscription.daysLeft} дн. Приятного пользования.`
    : "Оформите подписку, чтобы продолжить пользоваться мастер-панелью."
  const needsOnboarding = Boolean(onboarding && onboarding.completedCount < onboarding.totalCount)

  function copyPublicLink() {
    navigator.clipboard?.writeText(clientBotLink)
      .then(() => setCopyMessage("Ссылка скопирована"))
      .catch(() => setCopyMessage("Не удалось скопировать ссылку"))
  }

  const masterMenuItems = getMasterTopMenuItems(master.key)

  return (
    <main className="app">
      <AppTopBar
        subtitle="кабинет мастера"
        username={master.username}
        homeUrl={`/master/${master.key}`}
        infoUrl={`/master/${master.key}/info`}
        menuItems={masterMenuItems}
      />

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

      {shouldShowSubscriptionBanner && (
        <Link
          to={`/master/${master.key}/subscription`}
          className={`homeSubscriptionBanner ${subscription.hasAccess ? "active" : "expired"} ${isSubscriptionCountdown ? "countdown" : ""}`}
        >
          <span>
            {subscription.hasAccess ? <ShieldCheck size={24} strokeWidth={2.4} /> : <CreditCard size={24} strokeWidth={2.4} />}
          </span>
          <div>
            <strong>{subscriptionBannerTitle}</strong>
            <small>{subscriptionBannerText}</small>
          </div>
          {isSubscriptionCountdown && <b>{subscription.daysLeft}</b>}
        </Link>
      )}

      {needsOnboarding && onboarding && (
        <Link to={`/master/${master.key}/onboarding`} className="onboardingHomeBanner">
          <span>
            <LayoutDashboard size={24} strokeWidth={2.4} />
          </span>
          <div>
            <strong>Завершите настройку</strong>
            <small>{onboarding.completedCount} из {onboarding.totalCount} шагов готово. Следующий шаг: {onboarding.nextTitle.toLowerCase()}.</small>
            <i>
              <b style={{ width: `${onboarding.progressPercent}%` }} />
            </i>
          </div>
          <ChevronRight size={22} strokeWidth={2.5} />
        </Link>
      )}

      <ClientCabinetBanner telegramId={master.telegramId} />

      <section className="adminCard masterLinkCard">
        <div>
          <strong>Ваша ссылка</strong>
          <span>{clientBotLink}</span>
        </div>
        <button type="button" onClick={copyPublicLink}>Скопировать</button>
      </section>
      {copyMessage && <div className="profileMessage">{copyMessage}</div>}

      <section className="grid">
        <Link to={`/master/${master.key}/bookings`} className="cardLink">
          <Card icon={<CalendarCheck />} title="Записи" text="Скоро здесь появятся записи" />
        </Link>
        <Link to={`/master/${master.key}/clients`} className="cardLink">
          <Card icon={<Users />} title="Клиенты" text="База клиентов мастера" />
        </Link>
        <Link to={`/master/${master.key}/analytics`} className="cardLink">
          <Card icon={<BarChart3 />} title="Статистика" text="Доход, записи и клиенты" />
        </Link>
        <Link to={`/master/${master.key}/broadcast`} className="cardLink">
          <Card icon={<Megaphone />} title="Рассылка" text="Сообщения всем клиентам" />
        </Link>
        <Link to={`/master/${master.key}/services`} className="cardLink">
          <Card icon={<BriefcaseBusiness />} title="Услуги" text="Настройка услуг и цен" />
        </Link>
        <Link to={`/master/${master.key}/schedule`} className="cardLink">
          <Card icon={<CalendarDays />} title="Расписание" text="Управление временем" />
        </Link>
        <Link to={`/master/${master.key}/subscription`} className="cardLink">
          <Card icon={<CreditCard />} title="Подписка" text="Доступ к мастер-панели и тарифы" />
        </Link>
        <Link to={`/master/${master.key}/public-profile`} className="cardLink">
          <Card icon={<Globe />} title="Профиль" text="Так страницу будут видеть клиенты" />
        </Link>
      </section>

      <MasterBottomNav masterKey={master.key} />
    </main>
  )
}

function onboardingStepIcon(code: string) {
  if (code === "profile") return <User size={22} strokeWidth={2.4} />
  if (code === "avatar") return <Camera size={22} strokeWidth={2.4} />
  if (code === "services") return <BriefcaseBusiness size={22} strokeWidth={2.4} />
  if (code === "schedule") return <CalendarDays size={22} strokeWidth={2.4} />
  if (code === "blockTime") return <Clock size={22} strokeWidth={2.4} />
  if (code === "portfolio") return <ImageIcon size={22} strokeWidth={2.4} />
  if (code === "publicProfile") return <Globe size={22} strokeWidth={2.4} />
  if (code === "servicesRequired") return <BriefcaseBusiness size={22} strokeWidth={2.4} />
  if (code === "scheduleRequired") return <CalendarDays size={22} strokeWidth={2.4} />
  return <ShieldCheck size={22} strokeWidth={2.4} />
}

function MasterOnboardingPage() {
  const { key } = useParams()
  const [master, setMaster] = useState<Master | null>(null)
  const [onboarding, setOnboarding] = useState<MasterOnboarding | null>(null)
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState("")

  useEffect(() => {
    if (!key) return

    setLoading(true)
    setMessage("")

    fetch(`${API_URL}/api/master/${key}`)
      .then(async (res) => {
        if (!res.ok) throw new Error("Мастер не найден")
        return normalizeMaster(await res.json())
      })
      .then((masterData) => {
        setMaster(masterData)
        return fetch(`${API_URL}/api/master/${key}/onboarding`, {
          headers: { "X-Telegram-Id": telegramId() || String(masterData.telegramId) },
        })
      })
      .then(async (res) => {
        if (!res.ok) {
          const data = await res.json().catch(() => null)
          throw new Error(data?.message || "Не удалось загрузить онбординг")
        }

        return res.json()
      })
      .then((data) => setOnboarding(normalizeOnboarding(data)))
      .catch((err) => setMessage(err.message || "Ошибка загрузки онбординга"))
      .finally(() => setLoading(false))
  }, [key])

  if (loading) {
    return <ComingSoon title="Готовим старт" subtitle="Собираем шаги настройки мастера" />
  }

  if (!master || !onboarding) {
    return <ComingSoon title="Не получилось открыть старт" subtitle={message || "Попробуйте вернуться в мастер-панель"} />
  }

  return (
    <main className="app">
      <Link to={`/master/${master.key}`} className="subscriptionBackLink">
        <ArrowLeft size={18} strokeWidth={2.5} />
        <span>В мастер-панель</span>
      </Link>

      <section className="onboardingHero">
        <div className="onboardingHeroTop">
          <span>
            <BriefcaseBusiness size={34} strokeWidth={2.3} />
          </span>
          <b>{onboarding.progressPercent}%</b>
        </div>
        <h1>{onboarding.isComplete ? "Профиль готов к работе" : "Настройте профиль мастера"}</h1>
        <p>
          {onboarding.isComplete
            ? "Можно делиться ссылкой, принимать записи и возвращаться к настройкам в любой момент."
            : "Пройдите несколько коротких шагов, чтобы клиентам было понятно, кто вы, какие услуги доступны и когда можно записаться."}
        </p>
        <div className="onboardingProgress">
          <i>
            <b style={{ width: `${onboarding.progressPercent}%` }} />
          </i>
          <small>{onboarding.completedCount} из {onboarding.totalCount} шагов готово</small>
        </div>
        <div className="onboardingHeroActions">
          <Link to={onboarding.nextUrl || `/master/${master.key}`} className="primaryButton onboardingPrimaryLink">
            {onboarding.isComplete ? "Открыть панель" : "Продолжить настройку"}
          </Link>
          <Link to={`/master/${master.key}/public-profile`} className="secondaryButton onboardingSecondaryLink">
            Посмотреть профиль
          </Link>
        </div>
      </section>

      <section className="onboardingNextCard">
        <small>Следующий шаг</small>
        <strong>{onboarding.nextTitle}</strong>
        <p>{onboarding.nextText}</p>
      </section>

      <section className="onboardingImportantCard">
        <div className="onboardingSectionTitle">
          <strong>Важно для записи</strong>
          <small>Что влияет на то, сможет ли клиент записаться</small>
        </div>
        <div className="onboardingTips">
          {onboarding.tips.map((tip) => (
            <Link to={tip.url} className={`onboardingTip ${tip.severity}`} key={tip.code}>
              <span>{onboardingStepIcon(tip.code)}</span>
              <div>
                <strong>{tip.title}</strong>
                <small>{tip.text}</small>
              </div>
              <ChevronRight size={19} strokeWidth={2.5} />
            </Link>
          ))}
        </div>
      </section>

      <section className="onboardingSteps">
        {onboarding.steps.map((step, index) => (
          <Link to={step.url} className={`onboardingStep ${step.isDone ? "done" : ""} ${!step.isDone ? step.severity : ""}`} key={step.code}>
            <span className="onboardingStepNumber">{step.isDone ? <ShieldCheck size={21} strokeWidth={2.5} /> : index + 1}</span>
            <span className="onboardingStepIcon">{onboardingStepIcon(step.code)}</span>
            <span className="onboardingStepText">
              <strong>{step.title}</strong>
              <small>{step.text}</small>
              {!step.isDone && step.warningText && <b>{step.warningText}</b>}
            </span>
            {!step.isRequired && <em>Совет</em>}
            <ChevronRight size={20} strokeWidth={2.5} />
          </Link>
        ))}
      </section>

      <section className="adminCard onboardingHint">
        <strong>Можно делать не по порядку</strong>
        <p>Главное для старта: услуги, график и понятный профиль. Фото и портфолио можно добавить позже, но они повышают доверие.</p>
      </section>

      <MasterBottomNav masterKey={master.key} />
    </main>
  )
}

function MasterAnalyticsPage() {
  const { key } = useParams()
  const today = new Date()
  const monthStart = new Date(today.getFullYear(), today.getMonth(), 1)
  const monthEnd = new Date(today.getFullYear(), today.getMonth() + 1, 0)
  const [from, setFrom] = useState(formatDateInput(monthStart))
  const [to, setTo] = useState(formatDateInput(monthEnd))
  const [analytics, setAnalytics] = useState<MasterAnalytics | null>(null)
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState("")

  useEffect(() => {
    if (!key) return

    setLoading(true)
    setMessage("")

    fetch(`${API_URL}/api/master/${key}/analytics?from=${from}&to=${to}`, {
      headers: { "X-Telegram-Id": telegramId() },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok) {
          throw new Error(data?.message || "Не удалось загрузить статистику")
        }

        setAnalytics(normalizeMasterAnalytics(data))
      })
      .catch((err) => {
        setAnalytics(null)
        setMessage(err.message || "Ошибка загрузки статистики")
      })
      .finally(() => setLoading(false))
  }, [key, from, to])

  const periodDays = analytics ? getPeriodDays(analytics.from, analytics.to) : 0
  const maxCompletedBookings = Math.max(1, ...(analytics?.days.map((day) => day.completedBookings) ?? [0]))
  const chartPoints = analytics?.days.length
    ? analytics.days.map((day, index) => {
      const x = analytics.days.length === 1 ? 160 : 18 + (index * 284) / (analytics.days.length - 1)
      const y = 142 - (day.completedBookings / maxCompletedBookings) * 108
      return `${x},${y}`
    }).join(" ")
    : ""

  return (
    <main className="app">
      <AppTopBar
        subtitle="кабинет мастера"
        homeUrl={`/master/${key ?? ""}`}
        infoUrl={`/master/${key ?? ""}/info`}
        menuItems={getMasterTopMenuItems(key ?? "")}
      />

      <header className="adminHeader analyticsHeader">
        <h1>Статистика</h1>
        <p>Доход, записи и клиенты за выбранный период</p>
      </header>

      <section className="analyticsPeriodCard">
        <label>
          <span>Начало</span>
          <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
        </label>
        <label>
          <span>Конец</span>
          <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
        </label>
        <small>{periodDays > 0 ? `Выбрано ${periodDays} дн.` : "Выберите период"}</small>
      </section>

      {message && <div className="profileMessage">{message}</div>}

      <section className="analyticsSummaryGrid">
        <AnalyticsMetricCard
          title="Заработано"
          value={formatMoney(analytics?.completedRevenue ?? 0)}
          text="По завершенным записям"
          icon={<Banknote size={24} strokeWidth={2.4} />}
        />
        <AnalyticsMetricCard
          title="Потенциально"
          value={formatMoney(analytics?.potentialRevenue ?? 0)}
          text="По активным записям"
          icon={<CreditCard size={24} strokeWidth={2.4} />}
        />
      </section>

      <section className="analyticsNumbers">
        <AnalyticsNumber title="Завершенные записи" value={analytics?.completedBookings ?? 0} loading={loading} />
        <AnalyticsNumber title="Активные записи" value={analytics?.activeBookings ?? 0} loading={loading} />
        <AnalyticsNumber title="Клиенты" value={analytics?.clients ?? 0} loading={loading} />
      </section>

      <section className="analyticsChartCard">
        <div className="analyticsChartTitle">
          <div>
            <strong>Количество завершенных записей</strong>
            <small>Динамика по дням</small>
          </div>
          <span>{analytics?.completedBookings ?? 0}</span>
        </div>

        <svg viewBox="0 0 320 170" className="analyticsChart" role="img" aria-label="График завершенных записей">
          <line x1="18" y1="34" x2="302" y2="34" />
          <line x1="18" y1="88" x2="302" y2="88" />
          <line x1="18" y1="142" x2="302" y2="142" />
          {chartPoints ? (
            <>
              <polyline points={chartPoints} />
              {analytics?.days.map((day, index) => {
                const x = analytics.days.length === 1 ? 160 : 18 + (index * 284) / (analytics.days.length - 1)
                const y = 142 - (day.completedBookings / maxCompletedBookings) * 108
                return <circle key={day.date} cx={x} cy={y} r={4} />
              })}
            </>
          ) : (
            <text x="160" y="88" textAnchor="middle">Нет данных</text>
          )}
        </svg>
      </section>

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

const normalizeMasterAnalytics = (data: any): MasterAnalytics => ({
  from: data?.from ?? data?.From ?? "",
  to: data?.to ?? data?.To ?? "",
  completedRevenue: data?.completedRevenue ?? data?.CompletedRevenue ?? 0,
  potentialRevenue: data?.potentialRevenue ?? data?.PotentialRevenue ?? 0,
  completedBookings: data?.completedBookings ?? data?.CompletedBookings ?? 0,
  activeBookings: data?.activeBookings ?? data?.ActiveBookings ?? 0,
  clients: data?.clients ?? data?.Clients ?? 0,
  days: (data?.days ?? data?.Days ?? []).map((day: any) => ({
    date: day?.date ?? day?.Date ?? "",
    completedBookings: day?.completedBookings ?? day?.CompletedBookings ?? 0,
    completedRevenue: day?.completedRevenue ?? day?.CompletedRevenue ?? 0,
    potentialRevenue: day?.potentialRevenue ?? day?.PotentialRevenue ?? 0,
  })),
})

const formatMoney = (value: number) =>
  `${Math.round(value).toLocaleString("ru-RU")} ₽`

const getPeriodDays = (from: string, to: string) => {
  if (!from || !to) return 0

  const fromDate = new Date(from)
  const toDate = new Date(to)

  if (Number.isNaN(fromDate.getTime()) || Number.isNaN(toDate.getTime())) return 0

  return Math.max(1, Math.round((toDate.getTime() - fromDate.getTime()) / 86400000) + 1)
}

function AnalyticsMetricCard({
  title,
  value,
  text,
  icon,
}: {
  title: string
  value: string
  text: string
  icon: ReactNode
}) {
  return (
    <div className="analyticsMetricCard">
      <span>{icon}</span>
      <small>{title}</small>
      <strong>{value}</strong>
      <p>{text}</p>
    </div>
  )
}

function AnalyticsNumber({ title, value, loading }: { title: string; value: number; loading: boolean }) {
  return (
    <div className="analyticsNumber">
      <small>{title}</small>
      <strong>{loading ? "..." : value}</strong>
    </div>
  )
}

function MasterSubscriptionPage() {
  const { key } = useParams()
  const navigate = useNavigate()
  const [subscription, setSubscription] = useState<MasterSubscription | null>(null)
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState("")
  const [payingPlan, setPayingPlan] = useState("")

  useEffect(() => {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/subscription`)
      .then(async (res) => {
        if (!res.ok) {
          throw new Error("Не удалось загрузить подписку")
        }

        return res.json()
      })
      .then((data) => setSubscription(normalizeSubscription(data)))
      .catch((err) => setMessage(err.message || "Ошибка загрузки подписки"))
      .finally(() => setLoading(false))
  }, [key])

  function formatDate(value: string | null) {
    if (!value) return ""

    return new Date(value).toLocaleDateString("ru-RU", {
      day: "2-digit",
      month: "long",
      year: "numeric",
    })
  }

  async function planClick(plan: SubscriptionPlan) {
    if (!key) return

    setMessage("")
    setPayingPlan(plan.code)

    try {
      const res = await fetch(`${API_URL}/api/master/${key}/subscription/payments`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Telegram-Id": telegramId(),
        },
        body: JSON.stringify({ planCode: plan.code }),
      })

      const data = await res.json()

      if (!res.ok || !data.success) {
        throw new Error(data.message || "Не удалось создать платеж")
      }

      navigate(`/master/${key}/subscription/payment/${data.paymentToken}?open=1`)
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Ошибка создания платежа")
    } finally {
      setPayingPlan("")
    }
  }

  const subscriptionFeatures = [
    "Безлимитное управление услугами, ценами и расписанием",
    "Онлайн-запись клиентов через публичный профиль мастера",
    "Клиентская база, история посещений и ручное добавление клиентов",
    "Портфолио работ до 9 фото и фото профиля",
    "Рассылки клиентам и уведомления по записям",
  ]

  const statusTitle = subscription?.accessType === "founder"
    ? "Вы первый мастер"
    : subscription?.accessType === "paid"
      ? "Подписка активна"
      : subscription?.accessType === "trial"
        ? "Пробный период активен"
        : "Доступ закончился"

  const statusText = subscription?.accessType === "founder"
    ? "Поздравляем, у вас доступ навсегда. Приятного пользования."
    : subscription?.accessType === "paid"
      ? `Поздравляем, у вас подписка активна${subscription.subscriptionEndsAt ? ` до ${formatDate(subscription.subscriptionEndsAt)}` : ""}.`
      : subscription?.accessType === "trial"
        ? `У вас бесплатный период. Осталось ${subscription.daysLeft} дн.`
        : "Оформите подписку, чтобы продолжить пользоваться мастер-панелью."

  return (
    <main className="app">
      <Link to={`/master/${key ?? ""}`} className="subscriptionBackLink">
        <ArrowLeft size={18} strokeWidth={2.5} />
        <span>Назад в профиль</span>
      </Link>

      <header className="adminHeader">
        <h1>Подписка</h1>
        <p>Доступ мастера к панели и будущая оплата</p>
      </header>

      <section className={`subscriptionStatusCard ${subscription?.hasAccess ? "active" : "expired"}`}>
        <div className="subscriptionStatusIcon">
          {subscription?.hasAccess ? <ShieldCheck size={32} strokeWidth={2.4} /> : <CreditCard size={32} strokeWidth={2.4} />}
        </div>
        <div>
          <h2>{loading ? "Загружаем..." : statusTitle}</h2>
          <p>{loading ? "Проверяем статус доступа" : statusText}</p>
        </div>
      </section>

      {subscription?.accessType === "founder" && (
        <section className="adminCard subscriptionFounderCard">
          <strong>Статус: первый мастер</strong>
          <p>Этот доступ не заканчивается и не требует оплаты.</p>
        </section>
      )}

      {subscription && subscription.accessType !== "founder" && (
        <section className="subscriptionPlans">
          {subscription.availablePlans.map((plan) => {
            const monthlyPrice = Math.round(plan.priceRub / Math.max(plan.months, 1))
            const totalText = plan.months === 1
              ? "Оплата за 1 месяц"
              : `Итого ${plan.priceRub.toLocaleString("ru-RU")} ₽ за ${plan.title.toLowerCase()}`

            return (
              <button
                type="button"
                className="subscriptionPlanCard"
                key={plan.code}
                onClick={() => planClick(plan)}
                disabled={payingPlan === plan.code}
              >
                <span>{plan.title}</span>
                <strong>{monthlyPrice.toLocaleString("ru-RU")} ₽</strong>
                <em>в месяц</em>
                <p>{totalText}</p>
                <small>{payingPlan === plan.code ? "Создаем платеж..." : subscription.hasAccess ? "Продлить подписку" : "Оформить подписку"}</small>
              </button>
            )
          })}
        </section>
      )}

      {subscription && subscription.accessType !== "founder" && (
        <section className="adminCard subscriptionIncluded">
          <strong>Что входит в подписку</strong>
          <ul>
            {subscriptionFeatures.map((feature) => (
              <li key={feature}>
                <ShieldCheck size={17} strokeWidth={2.4} />
                <span>{feature}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {message && <div className="profileMessage">{message}</div>}

      {subscription?.accessType !== "founder" && (
        <section className="adminCard subscriptionNote">
          <strong>Как это будет работать</strong>
          <p>После успешной оплаты ЮKassa отправит уведомление, и подписка автоматически продлится. Если доступ уже активен, новый срок добавится сверху.</p>
        </section>
      )}

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function MasterSubscriptionPaymentPage() {
  const { key, paymentToken } = useParams()
  const [searchParams] = useSearchParams()
  const [payment, setPayment] = useState<SubscriptionPaymentStatus | null>(null)
  const [loading, setLoading] = useState(true)
  const [checking, setChecking] = useState(false)
  const [message, setMessage] = useState("")
  const openedPaymentRef = useRef(false)

  const isSucceeded = payment?.status === "succeeded"
  const isPending = !payment || payment.status === "pending"

  function openPayment(url?: string) {
    if (!url) return

    if (window.Telegram?.WebApp?.openLink) {
      window.Telegram.WebApp.openLink(url)
      return
    }

    window.location.href = url
  }

  async function loadPayment(showLoader = false) {
    if (!key || !paymentToken) return

    if (showLoader) setChecking(true)

    try {
      const res = await fetch(`${API_URL}/api/master/${key}/subscription/payments/${paymentToken}`, {
        headers: { "X-Telegram-Id": telegramId() },
      })
      const data = await res.json()

      if (!res.ok || data.success === false) {
        throw new Error(data.message || "Не удалось получить статус платежа")
      }

      setPayment(normalizePaymentStatus(data))
      setMessage("")
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Ошибка проверки платежа")
    } finally {
      setLoading(false)
      setChecking(false)
    }
  }

  useEffect(() => {
    loadPayment()
  }, [key, paymentToken])

  useEffect(() => {
    if (!payment || payment.status === "succeeded") return

    const timer = window.setInterval(() => {
      loadPayment()
    }, 4000)

    return () => window.clearInterval(timer)
  }, [payment?.status, key, paymentToken])

  useEffect(() => {
    if (!payment || openedPaymentRef.current || searchParams.get("open") !== "1") return

    openedPaymentRef.current = true
    openPayment(payment.confirmationUrl)
  }, [payment, searchParams])

  const title = loading
    ? "Проверяем платеж"
    : isSucceeded
      ? "Оплата прошла"
      : "Ожидаем оплату"

  const text = loading
    ? "Секунду, получаем статус от сервера."
    : isSucceeded
      ? "Спасибо! Подписка уже продлена, можно возвращаться в мастер-панель."
      : "После оплаты ЮKassa пришлет подтверждение, и мы автоматически обновим этот экран."

  return (
    <main className="app">
      <Link to={`/master/${key ?? ""}/subscription`} className="subscriptionBackLink">
        <ArrowLeft size={18} strokeWidth={2.5} />
        <span>Назад к подписке</span>
      </Link>

      <section className={`paymentStatusHero ${isSucceeded ? "success" : "pending"}`}>
        <div className="paymentStatusIcon">
          {isSucceeded ? <ShieldCheck size={34} strokeWidth={2.4} /> : <Clock size={34} strokeWidth={2.4} />}
        </div>
        <h1>{title}</h1>
        <p>{text}</p>
      </section>

      <section className="adminCard paymentDetailsCard">
        <div>
          <span>Тариф</span>
          <strong>{payment?.months ? `${payment.months} мес.` : "..."}</strong>
        </div>
        <div>
          <span>Сумма</span>
          <strong>{payment ? `${payment.amountRub.toLocaleString("ru-RU")} ₽` : "..."}</strong>
        </div>
        <div>
          <span>Статус</span>
          <strong>{isSucceeded ? "Оплачено" : isPending ? "Ожидает оплаты" : payment?.status}</strong>
        </div>
      </section>

      {!isSucceeded && (
        <section className="paymentActionCard">
          <button type="button" className="primaryButton" onClick={() => openPayment(payment?.confirmationUrl)} disabled={!payment?.confirmationUrl}>
            Перейти к оплате
          </button>
          <button type="button" className="secondaryButton" onClick={() => loadPayment(true)} disabled={checking}>
            {checking ? "Проверяем..." : "Я оплатил, проверить"}
          </button>
        </section>
      )}

      {isSucceeded && (
        <Link to={`/master/${key ?? ""}`} className="primaryButton paymentDoneLink">
          Вернуться в мастер-панель
        </Link>
      )}

      {message && <div className="profileMessage">{message}</div>}

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function PublicProfileStub() {
  const { key } = useParams()
  const currentTelegramId = telegramId()
  const clientCabinetUrl = currentTelegramId ? `/user/${currentTelegramId}` : "/"
  const [master, setMaster] = useState<Master | null>(null)
  const [services, setServices] = useState<MasterServiceItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState("")
  const [editMode, setEditMode] = useState<"name" | "description" | null>(null)
  const [draftName, setDraftName] = useState("")
  const [draftDescription, setDraftDescription] = useState("")
  const [profileMessage, setProfileMessage] = useState("")
  const [availableSlots, setAvailableSlots] = useState<PublicAvailableSlot[]>([])
  const [slotsLoading, setSlotsLoading] = useState(false)
  const [portfolioPhotos, setPortfolioPhotos] = useState<PortfolioPhoto[]>([])
  const [portfolioViewerIndex, setPortfolioViewerIndex] = useState<number | null>(null)

  useEffect(() => {
    if (!key) return

    Promise.all([
      fetch(`${API_URL}/api/master/${key}`).then((res) => {
        if (!res.ok) throw new Error("Мастер не найден")
        return res.json()
      }),
      fetch(`${API_URL}/api/master/${key}/services`).then((res) => {
        if (!res.ok) return []
        return res.json()
      }),
      fetch(`${API_URL}/api/master/${key}/portfolio`).then((res) => {
        if (!res.ok) return []
        return res.json()
      }),
    ])
      .then(([masterData, servicesData, portfolioData]) => {
        const normalizedMaster = normalizeMaster(masterData)
        setMaster(normalizedMaster)
        setDraftName(normalizedMaster.name || "")
        setDraftDescription(normalizedMaster.description || "")
        setServices(Array.isArray(servicesData) ? servicesData : [])
        setPortfolioPhotos(Array.isArray(portfolioData) ? portfolioData.map(normalizePortfolioPhoto) : [])
      })
      .catch(() => setError("Профиль мастера недоступен"))
      .finally(() => setLoading(false))
  }, [key])

  useEffect(() => {
    if (!key || services.length === 0) {
      setAvailableSlots([])
      return
    }

    const days = buildCalendarDays(45)

    setSlotsLoading(true)

    Promise.all(
      services.flatMap((service) =>
        days.map((day) =>
          fetch(`${API_URL}/api/public/master/${key}/slots?serviceId=${service.id}&date=${day.value}`)
            .then((res) => (res.ok ? res.json() : []))
            .then((slots) => ({
              day,
              service,
              slots: Array.isArray(slots) ? slots.filter((slot: BookingSlot) => !slot.isBusy).slice(0, 2) : [],
            }))
            .catch(() => ({ day, service, slots: [] }))
        )
      )
    )
      .then((items) => {
        const nextSlots = items
          .flatMap(({ day, service, slots }) =>
            slots.map((slot: BookingSlot) => ({
              serviceId: service.id,
              serviceName: service.name,
              date: day.value,
              label: `${day.weekday}, ${day.day} ${day.month}`,
              time: slot.time,
            }))
          )
          .sort((first, second) => `${first.date} ${first.time}`.localeCompare(`${second.date} ${second.time}`))
          .slice(0, 6)

        setAvailableSlots(nextSlots)
      })
      .finally(() => setSlotsLoading(false))
  }, [key, services])

  if (loading) {
    return <ComingSoon title="Загрузка..." subtitle="Открываем профиль мастера" />
  }

  if (error || !master) {
    return <ComingSoon title="Профиль недоступен" subtitle={error || "Мастер не найден"} />
  }

  const visibleServices = services.slice(0, 3)
  const description = master.description?.trim()
  const displayName = master.name?.trim() || master.username || "Мастер"
  const isOwner = currentTelegramId === String(master.telegramId)
  const visiblePortfolioPhotos = portfolioPhotos.slice(0, 6)
  const selectedPortfolioPhoto = portfolioViewerIndex === null ? null : portfolioPhotos[portfolioViewerIndex] ?? null

  function startEdit(field: "name" | "description") {
    setDraftName(master?.name || "")
    setDraftDescription(master?.description || "")
    setProfileMessage("")
    setEditMode(field)
  }

  function showProfileStub(section: string) {
    setProfileMessage(`${section} скоро можно будет редактировать здесь`)
  }

  function saveProfile() {
    if (!master || !currentTelegramId) return

    setProfileMessage("Сохраняем...")

    fetch(`${API_URL}/api/master/${master.key}/profile`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({
        name: draftName,
        description: draftDescription,
        paymentDetails: master.paymentDetails || "",
        phoneNumber: master.phoneNumber || "",
      }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось сохранить профиль")
        }

        setMaster({
          ...master,
          name: draftName.trim(),
          description: draftDescription.trim(),
        })
        setEditMode(null)
        setProfileMessage("Профиль сохранён")
      })
      .catch((err) => setProfileMessage(err.message || "Ошибка сохранения"))
  }

  return (
    <main className={`app publicProfilePage ${currentTelegramId ? "hasPublicBottomNav" : ""}`}>
      <header className="publicTopBar">
        <Link to={clientCabinetUrl} className="publicBackButton">
          ‹
        </Link>
        <div>
          <h1>Профиль мастера</h1>
          <p>Zapisi.Pro</p>
        </div>
        <span className="publicMenuDot">•••</span>
      </header>

      <section className="publicProfileHero">
        <div className="publicAvatar">
          {master.avatarUrl ? (
            <img src={master.avatarUrl} alt={displayName} />
          ) : (
            <User size={42} strokeWidth={2.1} />
          )}
        </div>
        <div className="publicMasterInfo">
          {editMode === "name" ? (
            <div className="publicEditBox">
              <input
                className="adminInput"
                value={draftName}
                onChange={(event) => setDraftName(event.target.value)}
                placeholder="Имя мастера"
                maxLength={50}
              />
              <div className="publicEditActions">
                <button type="button" onClick={saveProfile}>Сохранить</button>
                <button type="button" onClick={() => setEditMode(null)}>Отмена</button>
              </div>
            </div>
          ) : (
            <div className="editableTitle">
              <h2>{displayName}</h2>
              {isOwner && (
                <button type="button" className="editIconButton" onClick={() => startEdit("name")} aria-label="Редактировать имя">
                  <Pencil size={16} strokeWidth={2.4} />
                </button>
              )}
            </div>
          )}
          <p>@{master.username || "master"}</p>
          {master.phoneNumber && (
            <a className="publicPhoneLink" href={`tel:${master.phoneNumber}`}>
              <Phone size={15} strokeWidth={2.3} />
              {master.phoneNumber}
            </a>
          )}
          <span>Профиль в разработке</span>
        </div>
      </section>

      {profileMessage && <div className="profileMessage">{profileMessage}</div>}

      <section className="publicInfoCard">
        <div className="publicCardHeader">
          <h3>О мастере</h3>
          {isOwner && (
            <button type="button" className="editIconButton" onClick={() => startEdit("description")} aria-label="Редактировать описание">
              <Pencil size={16} strokeWidth={2.4} />
            </button>
          )}
        </div>
        {editMode === "description" ? (
          <div className="publicEditBox">
            <textarea
              className="adminInput publicTextarea"
              value={draftDescription}
              onChange={(event) => setDraftDescription(event.target.value)}
              placeholder="Описание мастера"
              maxLength={1000}
            />
            <div className="publicEditActions">
              <button type="button" onClick={saveProfile}>Сохранить</button>
              <button type="button" onClick={() => setEditMode(null)}>Отмена</button>
            </div>
          </div>
        ) : (
          <p>{description || "Описание скоро появится. Здесь мастер сможет рассказать о себе, опыте и подходе к работе."}</p>
        )}
      </section>

      <section className="publicInfoCard">
        <div className="publicCardHeader">
          <h3>Портфолио</h3>
          {isOwner && (
            <Link to={`/master/${master.key}/profile`} className="editIconButton" aria-label="Редактировать портфолио">
              <Pencil size={16} strokeWidth={2.4} />
            </Link>
          )}
        </div>
        {portfolioPhotos.length === 0 ? (
          <p>Работы мастера скоро появятся здесь.</p>
        ) : (
          <div className="publicPortfolioPreview">
            {visiblePortfolioPhotos.map((photo, index) => (
              <button
                type="button"
                className="publicPortfolioTile"
                key={photo.id}
                onClick={() => setPortfolioViewerIndex(index)}
                aria-label="Открыть фото портфолио"
              >
                <img src={photo.imageUrl} alt={`Работа мастера ${index + 1}`} />
                {index === visiblePortfolioPhotos.length - 1 && portfolioPhotos.length > visiblePortfolioPhotos.length && (
                  <span>+{portfolioPhotos.length - visiblePortfolioPhotos.length}</span>
                )}
              </button>
            ))}
          </div>
        )}
      </section>

      <section className="publicInfoCard">
        <div className="publicCardHeader">
          <h3>Услуги</h3>
          <div className="publicHeaderActions">
            {services.length > 3 && (
              <Link to={`/master/${master.key}/public-services`}>Открыть все</Link>
            )}
            {isOwner && (
              <button type="button" className="editIconButton" onClick={() => showProfileStub("Услуги")} aria-label="Редактировать услуги">
                <Pencil size={16} strokeWidth={2.4} />
              </button>
            )}
          </div>
        </div>
        <div className="publicServicesList">
          {services.length === 0 ? (
            <div className="emptyLine">Услуги пока не добавлены</div>
          ) : (
            visibleServices.map((service) => (
              <Link to={`/master/${master.key}/public-booking?serviceId=${service.id}`} className="publicServiceRow" key={service.id}>
                <span className="publicServiceIcon">
                  <BriefcaseBusiness size={20} strokeWidth={2.3} />
                </span>
                <div>
                  <strong>{service.name || "Услуга"}</strong>
                  <small>
                    {formatServicePrice(service)}
                    {service.prepaymentPercent > 0
                      ? ` · предоплата ${service.prepaymentAmount}₽ (${service.prepaymentPercent}%)`
                      : " · без предоплаты"}
                  </small>
                  {service.address && <small className="addressLine">📍 {service.address}</small>}
                </div>
                <ChevronRight size={21} strokeWidth={2.4} />
              </Link>
            ))
          )}
        </div>
      </section>

      <section className="publicInfoCard">
        <div className="publicCardHeader">
          <h3>Ближайшие окна</h3>
          {isOwner && (
            <button type="button" className="editIconButton" onClick={() => showProfileStub("Ближайшее время")} aria-label="Редактировать ближайшее время">
              <Pencil size={16} strokeWidth={2.4} />
            </button>
          )}
        </div>
        {services.length === 0 ? (
          <p>Добавьте услуги, чтобы клиенты могли видеть ближайшие свободные окна.</p>
        ) : slotsLoading ? (
          <div className="emptyLine">Ищем ближайшее свободное время...</div>
        ) : availableSlots.length === 0 ? (
          <p>Свободных окон на ближайшие дни пока нет.</p>
        ) : (
          <div className="publicSlotsGrid">
            {availableSlots.map((slot) => (
              <Link
                to={`/master/${master.key}/public-booking?serviceId=${slot.serviceId}&date=${slot.date}&time=${slot.time}`}
                className="publicSlotChip"
                key={`${slot.date}-${slot.time}`}
              >
                <CalendarCheck size={18} strokeWidth={2.3} />
                <span>{slot.label} · {slot.serviceName}</span>
                <strong>{slot.time}</strong>
              </Link>
            ))}
          </div>
        )}
      </section>

      <div className="publicActionBar">
        <Link to={`/master/${master.key}/public-booking`} className="publicPrimaryButton publicProfileAction">
          Записаться
        </Link>
      </div>

      {currentTelegramId && <PublicProfileBottomNav telegramId={currentTelegramId} />}

      {selectedPortfolioPhoto && (
        <div className="portfolioViewerOverlay">
          <button
            type="button"
            className="portfolioViewerClose"
            onClick={() => setPortfolioViewerIndex(null)}
            aria-label="Закрыть просмотр"
          >
            <X size={24} strokeWidth={2.4} />
          </button>
          <button
            type="button"
            className="portfolioViewerNav portfolioViewerPrev"
            disabled={portfolioViewerIndex === 0}
            onClick={() => setPortfolioViewerIndex((index) => index === null ? index : Math.max(0, index - 1))}
            aria-label="Предыдущее фото"
          >
            <ArrowLeft size={22} strokeWidth={2.4} />
          </button>
          <img src={selectedPortfolioPhoto.imageUrl} alt="Работа мастера" />
          <button
            type="button"
            className="portfolioViewerNav portfolioViewerNext"
            disabled={portfolioViewerIndex === portfolioPhotos.length - 1}
            onClick={() => setPortfolioViewerIndex((index) => index === null ? index : Math.min(portfolioPhotos.length - 1, index + 1))}
            aria-label="Следующее фото"
          >
            <ArrowRight size={22} strokeWidth={2.4} />
          </button>
          <div className="portfolioViewerCounter">
            {(portfolioViewerIndex ?? 0) + 1} / {portfolioPhotos.length}
          </div>
        </div>
      )}
    </main>
  )
}

function PublicServicesPage() {
  const { key } = useParams()
  const currentTelegramId = telegramId()
  const [services, setServices] = useState<MasterServiceItem[]>([])
  const [search, setSearch] = useState("")
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState("")

  useEffect(() => {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/services`)
      .then((res) => {
        if (!res.ok) throw new Error("Не удалось загрузить услуги")
        return res.json()
      })
      .then((data) => setServices(Array.isArray(data) ? data : []))
      .catch(() => setError("Услуги пока недоступны"))
      .finally(() => setLoading(false))
  }, [key])

  const query = search.trim().toLowerCase()
  const filteredServices = query
    ? services.filter((service) => service.name.toLowerCase().includes(query))
    : services

  return (
    <main className="app">
      <header className="publicTopBar">
        <Link to={`/master/${key}/public-profile`} className="publicBackButton">
          ‹
        </Link>
        <div>
          <h1>Услуги</h1>
          <p>Выберите услугу для записи</p>
        </div>
        <span className="publicMenuDot">•••</span>
      </header>

      <section className="publicInfoCard">
        <input
          className="adminInput searchInput"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Поиск по названию услуги"
        />
      </section>

      <section className="publicInfoCard">
        <div className="publicServicesList">
          {loading ? (
            <div className="emptyLine">Загружаем услуги...</div>
          ) : error ? (
            <div className="emptyLine">{error}</div>
          ) : filteredServices.length === 0 ? (
            <div className="emptyLine">Услуги не найдены</div>
          ) : (
            filteredServices.map((service) => (
              <div className="publicServiceRow publicServiceRowWithAction" key={service.id}>
                <span className="publicServiceIcon">
                  <BriefcaseBusiness size={20} strokeWidth={2.3} />
                </span>
                <div>
                  <strong>{service.name || "Услуга"}</strong>
                  <small>
                    {formatServicePrice(service)}
                    {service.prepaymentPercent > 0
                      ? ` · предоплата ${service.prepaymentAmount}₽ (${service.prepaymentPercent}%)`
                      : " · без предоплаты"}
                  </small>
                  {service.address && <small className="addressLine">📍 {service.address}</small>}
                </div>
                <Link to={`/master/${key}/public-booking?serviceId=${service.id}`} className="publicServiceBookButton">
                  Записаться
                </Link>
              </div>
            ))
          )}
        </div>
      </section>

      {currentTelegramId && <PublicProfileBottomNav telegramId={currentTelegramId} />}
    </main>
  )
}

function PublicBookingStub() {
  const { key } = useParams()
  const [searchParams] = useSearchParams()
  const currentTelegramId = telegramId()
  const telegramUser = window.Telegram?.WebApp?.initDataUnsafe?.user
  const [services, setServices] = useState<MasterServiceItem[]>([])
  const [selectedServiceId, setSelectedServiceId] = useState<number | null>(null)
  const [selectedDate, setSelectedDate] = useState(searchParams.get("date") || formatDateInput(new Date()))
  const [selectedTime, setSelectedTime] = useState(searchParams.get("time") || "")
  const [phoneNumber, setPhoneNumber] = useState("")
  const [slots, setSlots] = useState<BookingSlot[]>([])
  const [dateAvailability, setDateAvailability] = useState<Record<string, "loading" | "available" | "unavailable">>({})
  const [loadingServices, setLoadingServices] = useState(true)
  const [loadingSlots, setLoadingSlots] = useState(false)
  const [message, setMessage] = useState("")
  const [bookingResult, setBookingResult] = useState<BookingCreateResult | null>(null)

  useEffect(() => {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/services`)
      .then((res) => res.json())
      .then((data) => {
        const list = Array.isArray(data) ? data : []
        const serviceFromUrl = Number(searchParams.get("serviceId"))
        const hasServiceFromUrl = list.some((service: MasterServiceItem) => service.id === serviceFromUrl)

        setServices(list)
        setSelectedServiceId(hasServiceFromUrl ? serviceFromUrl : list[0]?.id ?? null)
      })
      .catch(() => setMessage("Не удалось загрузить услуги"))
      .finally(() => setLoadingServices(false))
  }, [key])

  useEffect(() => {
    if (!key || !selectedServiceId || !selectedDate) return

    setLoadingSlots(true)
    const timeFromUrl = searchParams.get("time") || ""
    setSelectedTime((currentTime) => (timeFromUrl && selectedDate === searchParams.get("date") ? timeFromUrl : currentTime))

    fetch(`${API_URL}/api/public/master/${key}/slots?serviceId=${selectedServiceId}&date=${selectedDate}`)
      .then((res) => res.json())
      .then((data) => {
        const nextSlots = Array.isArray(data) ? data : []
        setSlots(nextSlots)

        if (timeFromUrl && selectedDate === searchParams.get("date")) {
          const exists = nextSlots.some((slot: BookingSlot) => slot.time === timeFromUrl && !slot.isBusy)
          setSelectedTime(exists ? timeFromUrl : "")
          return
        }

        setSelectedTime("")
      })
      .catch(() => setSlots([]))
      .finally(() => setLoadingSlots(false))
  }, [key, selectedServiceId, selectedDate])

  const selectedService = services.find((service) => service.id === selectedServiceId) ?? null
  const calendarDays = buildCalendarDays(45)

  useEffect(() => {
    if (!key || !selectedServiceId) {
      setDateAvailability({})
      return
    }

    const days = buildCalendarDays(45)
    setDateAvailability(
      days.reduce<Record<string, "loading" | "available" | "unavailable">>((result, day) => {
        result[day.value] = "loading"
        return result
      }, {})
    )

    Promise.all(
      days.map((day) =>
        fetch(`${API_URL}/api/public/master/${key}/slots?serviceId=${selectedServiceId}&date=${day.value}`)
          .then((res) => (res.ok ? res.json() : []))
          .then((data) => ({
            day: day.value,
            status: Array.isArray(data) && data.some((slot: BookingSlot) => !slot.isBusy)
              ? "available"
              : "unavailable",
          }))
          .catch(() => ({ day: day.value, status: "unavailable" }))
      )
    ).then((items) => {
      const nextAvailability = items.reduce<Record<string, "available" | "unavailable">>((result, item) => {
        result[item.day] = item.status as "available" | "unavailable"
        return result
      }, {})

      setDateAvailability(nextAvailability)

      if (nextAvailability[selectedDate] !== "available") {
        const firstAvailable = days.find((day) => nextAvailability[day.value] === "available")

        if (firstAvailable) {
          setSelectedDate(firstAvailable.value)
        }
      }
    })
  }, [key, selectedServiceId])

  function createBooking() {
    if (!key || !selectedServiceId || !selectedTime || !currentTelegramId) {
      setMessage("Откройте запись из Telegram и выберите время")
      return
    }

    setMessage("Создаём запись...")

    fetch(`${API_URL}/api/user/${currentTelegramId}/bookings`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({
        masterKey: key,
        serviceId: selectedServiceId,
        date: selectedDate,
        time: selectedTime,
        username: telegramUser?.username ?? "unknown",
        phoneNumber: phoneNumber.trim(),
      }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось создать запись")
        }

        setBookingResult(data)
        setMessage(data.message || "Запись создана")
      })
      .catch((err) => setMessage(err.message || "Ошибка создания записи"))
  }

  function markPaid() {
    if (!bookingResult || !currentTelegramId) return

    setMessage("Отправляем мастеру подтверждение оплаты...")

    fetch(`${API_URL}/api/user/${currentTelegramId}/bookings/${bookingResult.bookingId}/paid`, {
      method: "POST",
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось отметить оплату")
        }

        setBookingResult({ ...bookingResult, status: "waiting_payment_confirm" })
        setMessage(data.message || "Ожидаем подтверждение оплаты от мастера")
      })
      .catch((err) => setMessage(err.message || "Ошибка подтверждения оплаты"))
  }

  return (
    <main className="app">
      <header className="publicTopBar">
        <Link to={`/master/${key}/public-profile`} className="publicBackButton">
          ‹
        </Link>
        <div>
          <h1>Запись к мастеру</h1>
          <p>Zapisi.Pro</p>
        </div>
        <span className="publicMenuDot">•••</span>
      </header>

      {message && <div className="profileMessage">{message}</div>}

      {bookingResult ? (
        <section className="publicInfoCard bookingResultCard">
          <span className="publicServiceIcon">
            <CalendarCheck size={24} strokeWidth={2.3} />
          </span>
          <h3>{bookingResult.status === "waiting_payment" ? "Нужна предоплата" : "Запись создана"}</h3>
          <p>
            {bookingResult.serviceName} · {selectedDate} · {selectedTime}
          </p>
          {bookingResult.address && <p className="addressLine">📍 {bookingResult.address}</p>}
          {bookingResult.status === "waiting_payment" && (
            <div className="paymentBox">
              <strong>Предоплата {bookingResult.prepaymentAmount}₽ ({bookingResult.prepaymentPercent}%)</strong>
              <span>{bookingResult.paymentDetails || "Реквизиты не указаны"}</span>
              <button type="button" onClick={markPaid}>Я оплатил</button>
            </div>
          )}
          {bookingResult.status === "waiting_payment_confirm" && (
            <p>Ожидаем подтверждение оплаты от мастера.</p>
          )}
          {bookingResult.status === "pending" && (
            <p>Мастеру отправлено сообщение. Ожидайте подтверждения.</p>
          )}
        </section>
      ) : (
        <>
          <section className="publicInfoCard">
            <h3>Услуга</h3>
            {loadingServices ? (
              <div className="emptyLine">Загружаем услуги...</div>
            ) : services.length === 0 ? (
              <div className="emptyLine">У мастера пока нет услуг</div>
            ) : (
              <div className="servicePicker">
                {services.map((service) => (
                  <button
                    type="button"
                    className={selectedServiceId === service.id ? "active" : ""}
                    key={service.id}
                    onClick={() => setSelectedServiceId(service.id)}
                  >
                    <strong>{service.name}</strong>
                    <span>
                      {formatServicePrice(service)} · {service.duration} мин
                      {service.prepaymentPercent > 0 ? ` · предоплата ${service.prepaymentAmount}₽` : ""}
                    </span>
                    {service.address && <span>📍 {service.address}</span>}
                  </button>
                ))}
              </div>
            )}          </section>

          <section className="publicInfoCard">
            <h3>Дата</h3>
            <div className="calendarStrip">
              {calendarDays.map((day) => {
                const availability = dateAvailability[day.value] ?? "loading"

                return (
                  <button
                    type="button"
                    className={`${selectedDate === day.value ? "active" : ""} ${availability}`}
                    key={day.value}
                    onClick={() => setSelectedDate(day.value)}
                  >
                    <span>{day.weekday}</span>
                    <strong>{day.day}</strong>
                    <small>{availability === "loading" ? "..." : availability === "available" ? "Есть" : "Нет мест"}</small>
                  </button>
                )
              })}
            </div>
          </section>

          <section className="publicInfoCard">
            <h3>Время</h3>
            {loadingSlots ? (
              <div className="emptyLine">Ищем свободное время...</div>
            ) : slots.length === 0 ? (
              <div className="emptyLine">На эту дату свободного времени нет</div>
            ) : (
              <div className="slotGrid">
                {slots.map((slot) => (
                  <button
                    type="button"
                    disabled={slot.isBusy}
                    className={selectedTime === slot.time ? "active" : ""}
                    key={slot.time}
                    onClick={() => setSelectedTime(slot.time)}
                  >
                    {slot.time}
                  </button>
                ))}
              </div>
            )}
          </section>

          <section className="publicInfoCard">
            <h3>Телефон</h3>
            <input
              className="adminInput"
              inputMode="tel"
              value={phoneNumber}
              onChange={(event) => setPhoneNumber(event.target.value)}
              placeholder="+7..."
            />
          </section>

          <button
            type="button"
            className="publicPrimaryButton bookingSubmitButton"
            disabled={!selectedService || !selectedTime}
            onClick={createBooking}
          >
            Записаться
          </button>
        </>
      )}

      <Link to={`/master/${key}/public-profile`} className="publicPrimaryButton">
        Вернуться в профиль
      </Link>

      {currentTelegramId && <PublicProfileBottomNav telegramId={currentTelegramId} />}
    </main>
  )
}

function MasterBookingsPage() {
  const { key } = useParams()
  const currentTelegramId = telegramId()
  const [bookings, setBookings] = useState<MasterBooking[]>([])
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState("")
  const [bookingMode, setBookingMode] = useState<"list" | "calendar">("list")
  const [showHistory, setShowHistory] = useState(false)
  const [selectedCalendarDate, setSelectedCalendarDate] = useState("")

  function loadBookings() {
    if (!key) return

    setLoading(true)
    setMessage("")

    fetch(`${API_URL}/api/master/${key}/bookings`, {
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok) {
          setBookings([])
          setMessage(data.message || "Не удалось загрузить записи")
          return
        }

        setBookings(Array.isArray(data) ? data : [])
      })
      .catch(() => {
        setBookings([])
        setMessage("Ошибка соединения с сервером")
      })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    loadBookings()
  }, [key])

  function updateBooking(bookingId: number, action: "accept" | "cancel" | "payment-accept" | "payment-reject") {
    if (!key) return

    const loadingTextByAction = {
      accept: "Подтверждаем запись...",
      cancel: "Отменяем запись...",
      "payment-accept": "Подтверждаем оплату...",
      "payment-reject": "Отклоняем оплату...",
    }

    setMessage(loadingTextByAction[action])

    fetch(`${API_URL}/api/master/${key}/bookings/${bookingId}/${action}`, {
      method: "POST",
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось обновить запись")
        }

        setMessage(data.message || "Запись обновлена")
        loadBookings()
      })
      .catch((err) => setMessage(err.message || "Ошибка обновления записи"))
  }

  function deleteTimeBlock(blockBookingId: number) {
    if (!key) return

    const blockId = Math.abs(blockBookingId)
    setMessage("Удаляем блокировку...")

    fetch(`${API_URL}/api/master/${key}/time-blocks/${blockId}`, {
      method: "DELETE",
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось удалить блокировку")
        }

        setMessage(data.message || "Блокировка удалена")
        loadBookings()
      })
      .catch((err) => setMessage(err.message || "Ошибка удаления блокировки"))
  }

  const activeBookings = bookings.filter((booking) => !isHistoryStatus(booking.status))
  const historyBookings = bookings.filter((booking) => isHistoryStatus(booking.status))
  const visibleBookings = showHistory ? historyBookings : activeBookings
  const calendarGroups = activeBookings.reduce<Record<string, MasterBooking[]>>((groups, booking) => {
    const { date } = splitBookingDateTime(booking.dateTime)
    groups[date] = [...(groups[date] ?? []), booking]
    return groups
  }, {})
  const calendarDates = Object.keys(calendarGroups).sort(compareBookingDates)
  const calendarDatesKey = calendarDates.join("|")
  const selectedDayBookings = selectedCalendarDate ? calendarGroups[selectedCalendarDate] ?? [] : []

  useEffect(() => {
    if (calendarDates.length === 0) {
      setSelectedCalendarDate("")
      return
    }

    if (!selectedCalendarDate || !calendarDates.includes(selectedCalendarDate)) {
      setSelectedCalendarDate(calendarDates[0])
    }
  }, [calendarDatesKey, selectedCalendarDate])

  function renderMasterBookingCard(booking: MasterBooking) {
    if (booking.isManualBlock) {
      const { time } = splitBookingDateTime(booking.dateTime)

      return (
        <div className="masterBookingCard manualBlockCard" key={`block-${booking.id}`}>
          <div className="bookingCardHeader">
            <div>
              <strong>{booking.serviceName || "Занято"}</strong>
              <span>Закрыто вручную</span>
            </div>
            <StatusBadge status={booking.status} />
          </div>

          <div className="bookingMetaGrid">
            <span>
              <CalendarCheck size={17} strokeWidth={2.2} />
              {booking.dateTime}
            </span>
            <span>
              <Clock size={17} strokeWidth={2.2} />
              c {time}
            </span>
          </div>

          <div className="bookingActions singleAction">
            <button type="button" className="dangerButton" onClick={() => deleteTimeBlock(booking.id)}>
              <Trash2 size={17} strokeWidth={2.3} />
              Удалить блокировку
            </button>
          </div>
        </div>
      )
    }

    return (
      <div className="masterBookingCard" key={booking.id}>
        <div className="bookingCardHeader">
          <div>
            <strong>{booking.serviceName || "Услуга"}</strong>
            <span>@{booking.clientUsername || booking.clientTelegramId}</span>
          </div>
          <StatusBadge status={booking.status} />
        </div>

        <div className="bookingMetaGrid">
          <span>
            <CalendarCheck size={17} strokeWidth={2.2} />
            {booking.dateTime}
          </span>
          <span>
            <Banknote size={17} strokeWidth={2.2} />
            {booking.price}₽
          </span>
          <span>
            <BadgePercent size={17} strokeWidth={2.2} />
            {booking.prepaymentPercent > 0
              ? `${booking.prepaymentAmount}₽ (${booking.prepaymentPercent}%)`
              : "без предоплаты"}
          </span>
          {booking.address && (
            <span>
              <MapPin size={17} strokeWidth={2.2} />
              {booking.address}
            </span>
          )}
        </div>

        {(booking.status === "pending" || booking.status === "confirmed" || booking.status === "waiting_payment_confirm") && (
          <div className="bookingActions">
            {booking.status === "pending" && (
              <button type="button" onClick={() => updateBooking(booking.id, "accept")}>
                Подтвердить
              </button>
            )}
            {booking.status === "waiting_payment_confirm" && (
              <>
                <button type="button" onClick={() => updateBooking(booking.id, "payment-accept")}>
                  Деньги пришли
                </button>
                <button type="button" className="dangerButton" onClick={() => updateBooking(booking.id, "payment-reject")}>
                  Не пришли
                </button>
              </>
            )}
            <button type="button" className="dangerButton" onClick={() => updateBooking(booking.id, "cancel")}>
              Отменить
            </button>
          </div>
        )}
      </div>
    )
  }

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Записи</h1>
        <p>Заявки клиентов и активные записи</p>
      </header>

      {message && <div className="profileMessage">{message}</div>}

      <section className="bookingToolbar">
        <div className="bookingModeSwitch">
          <button
            type="button"
            className={!showHistory && bookingMode === "list" ? "active" : ""}
            onClick={() => {
              setShowHistory(false)
              setBookingMode("list")
            }}
          >
            Обычный
          </button>
          <button
            type="button"
            className={!showHistory && bookingMode === "calendar" ? "active" : ""}
            onClick={() => {
              setShowHistory(false)
              setBookingMode("calendar")
            }}
          >
            График
          </button>
        </div>
        <button
          type="button"
          className={`historyIconButton ${showHistory ? "active" : ""}`}
          onClick={() => setShowHistory(!showHistory)}
          aria-label="История записей"
        >
          <Clock size={20} strokeWidth={2.3} />
          <span>{historyBookings.length}</span>
        </button>
      </section>

      <section className="adminCard masterBookingsList">
        {loading ? (
          <div className="emptyLine">Загружаем записи...</div>
        ) : visibleBookings.length === 0 ? (
          <div className="emptyLine">{showHistory ? "История пока пустая" : "Активных записей пока нет"}</div>
        ) : !showHistory && bookingMode === "calendar" ? (
          <div className="bookingCalendarPanel">
            <div className="bookingDateStrip">
              {calendarDates.map((date) => {
                const [day, month] = date.split(".")

                return (
                  <button
                    type="button"
                    className={selectedCalendarDate === date ? "active" : ""}
                    key={date}
                    onClick={() => setSelectedCalendarDate(date)}
                  >
                    <span>{getWeekdayLabel(date)}</span>
                    <strong>{day}</strong>
                    <small>{month}</small>
                  </button>
                )
              })}
            </div>

            <div className="bookingDayTimeline">
              <div className="bookingDayHeader">
                <span>{selectedCalendarDate ? getWeekdayLabel(selectedCalendarDate) : ""}</span>
                <strong>{selectedCalendarDate || "Выберите дату"}</strong>
              </div>

              {selectedDayBookings.length === 0 ? (
                <div className="emptyLine">На этот день записей нет</div>
              ) : (
                selectedDayBookings
                  .slice()
                  .sort((first, second) => splitBookingDateTime(first.dateTime).time.localeCompare(splitBookingDateTime(second.dateTime).time))
                  .map((booking) => {
                    const { time } = splitBookingDateTime(booking.dateTime)
                    return (
                      <div className="bookingTimelineRow" key={booking.id}>
                        <time>{time}</time>
                        <div className="bookingTimelineCard">
                          <div>
                            <strong>
                              {booking.isManualBlock ? booking.serviceName || "Занято" : `@${booking.clientUsername || booking.clientTelegramId}`}
                            </strong>
                            <small>{booking.isManualBlock ? "Закрыто вручную" : booking.serviceName || "Услуга"}</small>
                          </div>
                          <div className="timelineCardActions">
                            <StatusBadge status={booking.status} />
                            {booking.isManualBlock && (
                              <button type="button" aria-label="Удалить блокировку" onClick={() => deleteTimeBlock(booking.id)}>
                                <Trash2 size={17} strokeWidth={2.4} />
                              </button>
                            )}
                          </div>
                        </div>
                      </div>
                    )
                  })
              )}
            </div>
          </div>
        ) : (
          visibleBookings.map((booking) => renderMasterBookingCard(booking))
        )}
      </section>

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function MasterTimeBlockPage() {
  const { key } = useParams()
  const currentTelegramId = telegramId()
  const [title, setTitle] = useState("")
  const [selectedDate, setSelectedDate] = useState(formatDateInput(new Date()))
  const [startTime, setStartTime] = useState("09:00")
  const [endTime, setEndTime] = useState("10:00")
  const [message, setMessage] = useState("")
  const [saving, setSaving] = useState(false)
  const calendarDays = buildCalendarDays(45)

  function createBlock() {
    if (!key) return

    if (!title.trim()) {
      setMessage("Введите название события")
      return
    }

    if (!startTime || !endTime || endTime <= startTime) {
      setMessage("Время окончания должно быть позже начала")
      return
    }

    setSaving(true)
    setMessage("Проверяем время...")

    fetch(`${API_URL}/api/master/${key}/time-blocks`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({
        title: title.trim(),
        date: selectedDate,
        startTime,
        endTime,
      }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось заблокировать время")
        }

        setMessage(data.message || "Время заблокировано")
        setTitle("")
      })
      .catch((err) => setMessage(err.message || "Ошибка соединения с сервером"))
      .finally(() => setSaving(false))
  }

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Заблокировать время</h1>
        <p>Закройте окно, если клиент записался вне бота</p>
      </header>

      {message && <div className="profileMessage">{message}</div>}

      <section className="adminCard serviceFormCard">
        <label>
          <span className="fieldLabel">Дата</span>
          <div className="calendarStrip blockDateStrip">
            {calendarDays.map((day) => (
              <button
                type="button"
                className={selectedDate === day.value ? "active" : ""}
                key={day.value}
                onClick={() => setSelectedDate(day.value)}
              >
                <span>{day.weekday}</span>
                <strong>{day.day}</strong>
                <small>{day.month}</small>
              </button>
            ))}
          </div>
        </label>

        <label>
          <span className="fieldLabel">Название события</span>
          <input
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            placeholder="Например: запись из WhatsApp"
            maxLength={100}
          />
        </label>

        <div className="formGrid">
          <label>
            <span className="fieldLabel">Начало</span>
            <input type="time" value={startTime} onChange={(event) => setStartTime(event.target.value)} />
          </label>
          <label>
            <span className="fieldLabel">Конец</span>
            <input type="time" value={endTime} onChange={(event) => setEndTime(event.target.value)} />
          </label>
        </div>

        <button type="button" className="primaryButton iconButton" onClick={createBlock} disabled={saving}>
          <Plus size={19} strokeWidth={2.4} />
          {saving ? "Сохраняем..." : "Заблокировать время"}
        </button>
      </section>

      <section className="publicInfoCard">
        <h3>Как это работает</h3>
        <p>Это время станет занятым для клиентов и появится в календаре мастера как ручная блокировка.</p>
      </section>

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function MasterSchedulePage() {
  const { key } = useParams()
  const currentTelegramId = telegramId()
  const [mode, setMode] = useState<"stable" | "manual">("stable")
  const [schedule, setSchedule] = useState<MasterScheduleDay[]>([])
  const [editingDay, setEditingDay] = useState<MasterScheduleDay | null>(null)
  const [startTime, setStartTime] = useState("")
  const [endTime, setEndTime] = useState("")
  const [isActive, setIsActive] = useState(true)
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState("")
  const [manualSlots, setManualSlots] = useState<MasterManualSlot[]>([])
  const [selectedManualDate, setSelectedManualDate] = useState(formatDateInput(new Date()))
  const [manualStartTime, setManualStartTime] = useState("09:00")
  const [manualEndTime, setManualEndTime] = useState("10:00")
  const [manualLoading, setManualLoading] = useState(false)
  const manualDays = buildCalendarDays(45)

  function loadSchedule() {
    if (!key) return

    setLoading(true)
    setMessage("")

    fetch(`${API_URL}/api/master/${key}/schedule`, {
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok) {
          setSchedule([])
          setMessage(data.message || "Не удалось загрузить расписание")
          return
        }

        setSchedule(Array.isArray(data) ? data : [])
      })
      .catch(() => {
        setSchedule([])
        setMessage("Ошибка соединения с сервером")
      })
      .finally(() => setLoading(false))
  }

  function loadScheduleMode() {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/schedule-mode`, {
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)
        if (!res.ok) return
        setMode(data?.mode === "manual" ? "manual" : "stable")
      })
      .catch(() => undefined)
  }

  function loadManualSlots(date = selectedManualDate) {
    if (!key) return

    setManualLoading(true)

    fetch(`${API_URL}/api/master/${key}/manual-slots?date=${date}`, {
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => [])

        if (!res.ok) {
          setManualSlots([])
          return
        }

        setManualSlots(Array.isArray(data) ? data : [])
      })
      .catch(() => setManualSlots([]))
      .finally(() => setManualLoading(false))
  }

  useEffect(() => {
    loadSchedule()
    loadScheduleMode()
  }, [key])

  useEffect(() => {
    if (mode === "manual") {
      loadManualSlots(selectedManualDate)
    }
  }, [mode, selectedManualDate, key])

  function changeMode(nextMode: "stable" | "manual") {
    if (!key) return

    setMode(nextMode)
    setMessage("")

    fetch(`${API_URL}/api/master/${key}/schedule-mode`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({ mode: nextMode }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)
        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось переключить режим")
        }
        setMessage(data.message || "Режим расписания обновлен")
      })
      .catch((err) => setMessage(err.message || "Ошибка сохранения режима"))
  }

  function addManualSlot() {
    if (!key) return

    setMessage("Добавляем слот...")

    fetch(`${API_URL}/api/master/${key}/manual-slots`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({
        date: selectedManualDate,
        startTime: manualStartTime,
        endTime: manualEndTime,
      }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)
        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось добавить слот")
        }
        setMessage(data.message || "Слот добавлен")
        loadManualSlots(selectedManualDate)
      })
      .catch((err) => setMessage(err.message || "Ошибка добавления слота"))
  }

  function deleteManualSlot(slotId: number) {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/manual-slots/${slotId}`, {
      method: "DELETE",
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)
        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось удалить слот")
        }
        setMessage(data.message || "Слот удален")
        loadManualSlots(selectedManualDate)
      })
      .catch((err) => setMessage(err.message || "Ошибка удаления слота"))
  }

  function clearManualDay() {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/manual-slots?date=${selectedManualDate}`, {
      method: "DELETE",
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)
        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось очистить день")
        }
        setMessage(data.message || "День очищен")
        loadManualSlots(selectedManualDate)
      })
      .catch((err) => setMessage(err.message || "Ошибка очистки дня"))
  }

  function startEditDay(day: MasterScheduleDay) {
    setEditingDay(day)
    setStartTime(day.startTime || "09:00")
    setEndTime(day.endTime || "18:00")
    setIsActive(day.isActive)
    setMessage("")
  }

  function saveDay() {
    if (!key || !editingDay) return

    setMessage("Сохраняем расписание...")

    fetch(`${API_URL}/api/master/${key}/schedule/${editingDay.dayOfWeek}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({
        startTime,
        endTime,
        isActive,
      }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось сохранить расписание")
        }

        setEditingDay(null)
        setMessage(data.message || "Расписание обновлено")
        loadSchedule()
      })
      .catch((err) => setMessage(err.message || "Ошибка сохранения"))
  }

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Расписание</h1>
        <p>Настройка графика работы мастера</p>
      </header>

      <section className="scheduleModeSwitch">
        <button
          type="button"
          className={mode === "stable" ? "active" : ""}
          onClick={() => changeMode("stable")}
        >
          Стабильное
        </button>
        <button
          type="button"
          className={mode === "manual" ? "active" : ""}
          onClick={() => changeMode("manual")}
        >
          Ручное
        </button>
      </section>

      {message && <div className="profileMessage">{message}</div>}

      {mode === "manual" ? (
        <section className="adminCard manualSchedulePanel">
          <div className="calendarStrip manualDateStrip">
            {manualDays.map((day) => (
              <button
                type="button"
                key={day.value}
                className={selectedManualDate === day.value ? "active" : ""}
                onClick={() => setSelectedManualDate(day.value)}
              >
                <span>{day.weekday}</span>
                <strong>{day.day}</strong>
                <small>{day.month}</small>
              </button>
            ))}
          </div>

          <div className="manualScheduleHeader">
            <div>
              <h2>Ручной график</h2>
              <p>Заполняйте слоты для выбранного дня</p>
            </div>
            <span>
              <CalendarDays size={24} strokeWidth={2.4} />
            </span>
          </div>

          <div className="manualSlotList">
            {manualLoading ? (
              <div className="manualEmptySlot">Загружаем слоты...</div>
            ) : manualSlots.length === 0 ? (
              <div className="manualEmptySlot">
                <CalendarDays size={22} strokeWidth={2.4} />
                <div>
                  <strong>Пока нет слотов</strong>
                  <small>Добавьте время вручную</small>
                </div>
              </div>
            ) : (
              manualSlots.map((slot) => (
                <div className="manualSlotRow" key={slot.id}>
                  <span>
                    <Clock size={20} strokeWidth={2.4} />
                  </span>
                  <strong>{slot.startTime} - {slot.endTime}</strong>
                  <button type="button" aria-label="Удалить слот" onClick={() => deleteManualSlot(slot.id)}>
                    <Trash2 size={19} strokeWidth={2.4} />
                  </button>
                </div>
              ))
            )}
          </div>

          <div className="manualSlotForm">
            <div className="timeGrid">
              <label>
                <span>Начало</span>
                <input
                  className="adminInput"
                  type="time"
                  value={manualStartTime}
                  onChange={(event) => setManualStartTime(event.target.value)}
                />
              </label>
              <label>
                <span>Конец</span>
                <input
                  className="adminInput"
                  type="time"
                  value={manualEndTime}
                  onChange={(event) => setManualEndTime(event.target.value)}
                />
              </label>
            </div>

            <button type="button" className="primaryButton iconButton" onClick={addManualSlot}>
              <Plus size={19} strokeWidth={2.4} />
              Добавить слот
            </button>
            <button type="button" className="clearDayButton" onClick={clearManualDay}>
              <Trash2 size={17} strokeWidth={2.4} />
              Очистить день
            </button>
          </div>
          <div className="stubIcon">
            <CalendarDays size={38} strokeWidth={2.2} />
          </div>
          <h2>Ручной режим в разработке</h2>
          <p>Позже здесь будет календарь месяца, редактирование дней, промежутков и отдельных ячеек времени.</p>
        </section>
      ) : (
        <section className="adminCard scheduleList">
          {loading ? (
            <div className="emptyLine">Загружаем расписание...</div>
          ) : schedule.length === 0 ? (
            <div className="emptyLine">Расписание пока не настроено</div>
          ) : (
            schedule.map((day) => (
              <div className="scheduleDayCard" key={day.dayOfWeek}>
                <div>
                  <strong>{day.dayName}</strong>
                  <span>{day.isActive ? `${day.startTime} - ${day.endTime}` : "Выходной"}</span>
                </div>
                <button type="button" onClick={() => startEditDay(day)}>
                  Редактировать
                </button>
              </div>
            ))
          )}
        </section>
      )}

      {editingDay && (
        <div className="modalOverlay">
          <div className="modal">
            <h2>{editingDay.dayName}</h2>
            <label className="toggleRow">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(event) => setIsActive(event.target.checked)}
              />
              Рабочий день
            </label>
            <div className="timeGrid">
              <label>
                <span>Начало</span>
                <input
                  className="adminInput"
                  type="time"
                  value={startTime}
                  onChange={(event) => setStartTime(event.target.value)}
                />
              </label>
              <label>
                <span>Конец</span>
                <input
                  className="adminInput"
                  type="time"
                  value={endTime}
                  onChange={(event) => setEndTime(event.target.value)}
                />
              </label>
            </div>
            <div className="modalActions">
              <button type="button" className="cancelButton" onClick={() => setEditingDay(null)}>
                Отмена
              </button>
              <button type="button" className="saveButton" onClick={saveDay}>
                Сохранить
              </button>
            </div>
          </div>
        </div>
      )}

      <MasterBottomNav masterKey={key ?? ""} />
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

        setDashboard({
          ...data,
          masters: Array.isArray(data.masters) ? data.masters.map(normalizeUserMaster) : [],
        })
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

  const userMenuItems = getUserTopMenuItems(dashboard.profile.telegramId)

  return (
    <main className="app">
      <AppTopBar
        subtitle="кабинет клиента"
        username={dashboard.profile.username}
        homeUrl={`/user/${dashboard.profile.telegramId}`}
        infoUrl={`/user/${dashboard.profile.telegramId}/info`}
        menuItems={userMenuItems}
      />

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

      {!dashboard.roles?.isMaster && (
        <Link to={`/user/${dashboard.profile.telegramId}/become-master`} className="becomeMasterBanner">
          <span>
            <BriefcaseBusiness size={26} strokeWidth={2.4} />
          </span>
          <div>
            <strong>Стать мастером в Zapisi.Pro</strong>
            <small>30 дней бесплатно: записи, напоминания, расписание и клиентская база</small>
          </div>
          <ChevronRight size={22} strokeWidth={2.4} />
        </Link>
      )}

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

function InformationPage({ mode }: { mode: InfoMode }) {
  const { telegramId: routeTelegramId, key } = useParams()
  const [activeTab, setActiveTab] = useState<InfoTabId>("privacy")
  const isMaster = mode === "master"
  const homeUrl = isMaster ? `/master/${key ?? ""}` : `/user/${routeTelegramId ?? ""}`
  const infoUrl = isMaster ? `/master/${key ?? ""}/info` : `/user/${routeTelegramId ?? ""}/info`
  const menuItems = isMaster
    ? getMasterTopMenuItems(key ?? "")
    : getUserTopMenuItems(Number(routeTelegramId ?? 0))
  const title = isMaster ? "Информация для мастера" : "Информация для клиента"
  const tabs: Array<{ id: InfoTabId; title: string; icon: ReactNode }> = [
    { id: "faq", title: "FAQ", icon: <CircleHelp size={19} strokeWidth={2.3} /> },
    { id: "rules", title: "Правила", icon: <FileText size={19} strokeWidth={2.3} /> },
    { id: "privacy", title: "Конфиденциальность", icon: <Shield size={19} strokeWidth={2.3} /> },
    { id: "offer", title: "Оферта", icon: <FileText size={19} strokeWidth={2.3} /> },
    { id: "statuses", title: "Статусы", icon: <Star size={19} strokeWidth={2.3} /> },
  ]

  const content = {
    faq: {
      title: "FAQ",
      items: isMaster
        ? [
          "Как настроить профиль: добавьте имя, описание, фото, услуги и расписание.",
          "Как получать записи: отправьте клиентам публичную ссылку из мастер-панели.",
          "Как работает подписка: доступ активен во время trial, оплаченного периода или статуса первого мастера.",
        ]
        : [
          "Как записаться: откройте профиль мастера, выберите услугу, дату и время.",
          "Где смотреть записи: раздел 'Мои записи' показывает будущие и прошлые визиты.",
          "Как стать мастером: нажмите 'Стать мастером' и создайте свой публичный ключ.",
        ],
    },
    rules: {
      title: "Правила сервиса",
      items: [
        "Используйте сервис только для реальных записей и корректных данных.",
        "Не передавайте чужие персональные данные без законного основания.",
        "Мастер отвечает за актуальность услуг, цен, адресов и расписания.",
      ],
    },
    privacy: {
      title: "Конфиденциальность",
      items: [
        "Zapisi.Pro обрабатывает Telegram ID, username, записи, контактные данные и данные профилей.",
        "Фотографии профиля и портфолио используются для отображения публичного профиля мастера.",
        "Данные применяются для работы записи, уведомлений, клиентской базы и поддержки.",
      ],
    },
    offer: {
      title: "Оферта",
      items: [
        "Полный текст оферты будет добавлен перед запуском оплаты.",
        "После подключения оплаты условия подписки будут отображаться в разделе 'Подписка'.",
        "Продолжение использования сервиса означает согласие с актуальными условиями.",
      ],
    },
    statuses: {
      title: "Статусы",
      items: [
        "Пробный период - временный бесплатный доступ к мастер-панели.",
        "Подписка активна - доступ оплачен и действует до указанной даты.",
        "Первый мастер - бессрочный доступ, выданный администрацией.",
        "Доступ закончился - рабочие функции мастера закрыты до продления.",
      ],
    },
  }[activeTab]

  return (
    <main className="app">
      <AppTopBar
        subtitle={isMaster ? "кабинет мастера" : "кабинет клиента"}
        homeUrl={homeUrl}
        infoUrl={infoUrl}
        menuItems={menuItems}
      />

      <section className="infoHeader">
        <Info size={30} strokeWidth={2.4} />
        <div>
          <h1>Информация</h1>
          <p>{title}</p>
        </div>
      </section>

      <section className="infoTabs">
        {tabs.map((tab) => (
          <button
            type="button"
            className={activeTab === tab.id ? "active" : ""}
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.icon}
            <span>{tab.title}</span>
          </button>
        ))}
      </section>

      <section className="infoContentCard">
        <h2>{content.title}</h2>
        <ol>
          {content.items.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ol>
      </section>

      {isMaster ? (
        <MasterBottomNav masterKey={key ?? ""} />
      ) : (
        <UserBottomNav telegramId={Number(routeTelegramId ?? 0)} />
      )}
    </main>
  )
}

function BecomeMasterPage() {
  const { telegramId: routeTelegramId } = useParams()
  const currentTelegramId = routeTelegramId || telegramId()
  const [keyValue, setKeyValue] = useState("")
  const [message, setMessage] = useState("")
  const [creating, setCreating] = useState(false)
  const [createdKey, setCreatedKey] = useState("")
  const [keyStatus, setKeyStatus] = useState<"idle" | "checking" | "available" | "unavailable">("idle")
  const [keyStatusMessage, setKeyStatusMessage] = useState("")

  useEffect(() => {
    if (!keyValue && window.Telegram?.WebApp?.initDataUnsafe?.user?.username) {
      setKeyValue(String(window.Telegram.WebApp.initDataUnsafe.user.username))
    }
  }, [keyValue])

  function normalizeKey(value: string) {
    return value
      .trim()
      .replace(/^@+/, "")
      .toLowerCase()
      .replace(/[^a-z0-9_-]/g, "")
      .slice(0, 32)
  }

  useEffect(() => {
    const key = normalizeKey(keyValue)

    if (!key) {
      setKeyStatus("idle")
      setKeyStatusMessage("")
      return
    }

    if (key.length < 4) {
      setKeyStatus("unavailable")
      setKeyStatusMessage("Минимум 4 символа")
      return
    }

    setKeyStatus("checking")
    setKeyStatusMessage("Проверяем ключ...")

    const timer = window.setTimeout(() => {
      fetch(`${API_URL}/api/master-key/check?key=${encodeURIComponent(key)}`)
        .then((res) => res.json())
        .then((data) => {
          const available = data?.available ?? data?.Available ?? false
          const message = data?.message ?? data?.Message ?? ""

          setKeyStatus(available ? "available" : "unavailable")
          setKeyStatusMessage(message || (available ? "Ключ свободен" : "Ключ занят"))
        })
        .catch(() => {
          setKeyStatus("unavailable")
          setKeyStatusMessage("Не удалось проверить ключ")
        })
    }, 350)

    return () => window.clearTimeout(timer)
  }, [keyValue])

  function createMasterProfile() {
    if (!currentTelegramId) {
      setMessage("Откройте регистрацию из Telegram, чтобы мы поняли ваш аккаунт.")
      return
    }

    const key = normalizeKey(keyValue)

    if (key.length < 4) {
      setMessage("Ключ должен быть минимум 4 символа")
      return
    }

    if (keyStatus !== "available") {
      setMessage("Выберите свободный ключ профиля")
      return
    }

    setCreating(true)
    setMessage("Создаём мастер-профиль...")

    fetch(`${API_URL}/api/user/${currentTelegramId}/become-master`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({ key }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось создать мастер-профиль")
        }

        setCreatedKey(data.masterKey || data.MasterKey || key)
        setMessage(data.message || "Мастер-профиль создан")
      })
      .catch((err) => setMessage(err.message || "Ошибка создания профиля"))
      .finally(() => setCreating(false))
  }

  const previewKey = normalizeKey(keyValue) || "your_name"
  const targetKey = createdKey || previewKey
  const backUrl = currentTelegramId ? `/user/${currentTelegramId}` : "/"

  return (
    <main className="app">
      <Link to={backUrl} className="subscriptionBackLink">
        <ArrowLeft size={18} strokeWidth={2.5} />
        <span>Назад в кабинет</span>
      </Link>

      <header className="adminHeader">
        <h1>Стать мастером</h1>
        <p>Запустите онлайн-запись за несколько минут</p>
      </header>

      <section className="becomeMasterHero">
        <div className="becomeMasterIcon">
          <BriefcaseBusiness size={34} strokeWidth={2.4} />
        </div>
        <h2>Zapisi.Pro поможет вам принимать записи без чатов и блокнотов</h2>
        <p>Автоматические напоминания, график, свободные окна, клиентская база, услуги, портфолио и публичный профиль в одной мини-апп панели.</p>
      </section>

      <section className="adminCard becomeMasterBenefits">
        <strong>Что вы получите</strong>
        <ul>
          <li><CalendarCheck size={18} strokeWidth={2.4} /> Клиенты сами выбирают услугу, дату и время</li>
          <li><Clock size={18} strokeWidth={2.4} /> Расписание и свободные окна всегда под рукой</li>
          <li><Send size={18} strokeWidth={2.4} /> Напоминания и уведомления по записям</li>
          <li><Users size={18} strokeWidth={2.4} /> База клиентов и история посещений</li>
          <li><ImageIcon size={18} strokeWidth={2.4} /> Портфолио работ и красивый публичный профиль</li>
        </ul>
      </section>

      <section className="trialNotice">
        <BadgePercent size={24} strokeWidth={2.4} />
        <div>
          <strong>30 дней бесплатно</strong>
          <small>Вам выдаётся пробный период. Мы заранее напомним об окончании, чтобы вы успели оформить подписку.</small>
        </div>
      </section>

      <section className="adminCard becomeMasterForm">
        <label className="fieldLabel" htmlFor="masterPublicKey">Ваш ключ профиля</label>
        <input
          id="masterPublicKey"
          className="adminInput"
          value={keyValue}
          onChange={(event) => setKeyValue(normalizeKey(event.target.value))}
          placeholder="Например: anna_nails"
        />
        {keyStatusMessage && (
          <div className={`keyStatus ${keyStatus}`}>
            {keyStatusMessage}
          </div>
        )}
        <div className="keyPreview">
          <span>Ваш профиль будет:</span>
          <strong>app-zapisi-pro.site/master/{previewKey}</strong>
        </div>
        <button
          type="button"
          className="primaryButton"
          onClick={createMasterProfile}
          disabled={creating || Boolean(createdKey) || keyStatus !== "available"}
        >
          {createdKey ? "Профиль создан" : creating ? "Создаём..." : "Создать мастер-профиль"}
        </button>
      </section>

      {message && <div className="profileMessage">{message}</div>}

      {createdKey && (
        <Link to={`/master/${targetKey}`} className="clientCabinetBanner">
          <span className="clientCabinetIcon">
            <BriefcaseBusiness size={22} strokeWidth={2.3} />
          </span>
          <span className="clientCabinetText">
            <strong>Открыть мастер-панель</strong>
            <small>Настройте профиль, услуги и расписание</small>
          </span>
          <ChevronRight size={22} strokeWidth={2.4} />
        </Link>
      )}
    </main>
  )
}

function UserBookingsPage() {
  const { dashboard, loading, error } = useUserDashboard()
  const [message, setMessage] = useState("")
  const [cancelledBookingIds, setCancelledBookingIds] = useState<number[]>([])
  const [bookingView, setBookingView] = useState<"active" | "history">("active")

  if (loading) {
    return <ComingSoon title="Загрузка..." subtitle="Получаем ваши записи" />
  }

  if (error || !dashboard) {
    return <ComingSoon title="Доступ закрыт" subtitle={error || "Записи недоступны"} />
  }

  const userTelegramId = dashboard.profile.telegramId

  function cancelBooking(bookingId: number) {
    setMessage("Отменяем запись...")

    fetch(`${API_URL}/api/user/${userTelegramId}/bookings/${bookingId}/cancel`, {
      method: "POST",
      headers: { "X-Telegram-Id": String(userTelegramId) },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось отменить запись")
        }

        setCancelledBookingIds((ids) => [...ids, bookingId])
        setMessage(data.message || "Запись отменена")
      })
      .catch((err) => setMessage(err.message || "Ошибка отмены записи"))
  }

  const bookings = dashboard.bookings.map((booking) =>
    cancelledBookingIds.includes(booking.id)
      ? { ...booking, status: "cancelled" }
      : booking
  )
  const activeBookings = bookings.filter((booking) => !isHistoryStatus(booking.status))
  const historyBookings = bookings.filter((booking) => isHistoryStatus(booking.status))
  const visibleBookings = bookingView === "active" ? activeBookings : historyBookings

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Мои записи</h1>
        <p>Все ваши записи у мастеров</p>
      </header>

      {message && <div className="profileMessage">{message}</div>}

      <section className="bookingTabs">
        <button
          type="button"
          className={bookingView === "active" ? "active" : ""}
          onClick={() => setBookingView("active")}
        >
          Активные
          <span>{activeBookings.length}</span>
        </button>
        <button
          type="button"
          className={bookingView === "history" ? "active" : ""}
          onClick={() => setBookingView("history")}
        >
          История
          <span>{historyBookings.length}</span>
        </button>
      </section>

      <UserBookingsSection
        title={bookingView === "active" ? "Активные записи" : "История записей"}
        bookings={visibleBookings}
        emptyText={bookingView === "active" ? "Активных записей пока нет" : "История пока пустая"}
        onCancel={bookingView === "active" ? cancelBooking : undefined}
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
  onCancel,
}: {
  title: string
  bookings: UserBooking[]
  emptyText: string
  onCancel?: (bookingId: number) => void
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
                {booking.address && <span>📍 {booking.address}</span>}
                {onCancel && booking.status !== "cancelled" && booking.status !== "completed" && (
                  <button type="button" className="inlineDangerButton" onClick={() => onCancel(booking.id)}>
                    <XCircle size={15} strokeWidth={2.4} />
                    <span>Отменить</span>
                  </button>
                )}
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
                {master.avatarUrl ? (
                  <img src={master.avatarUrl} alt={master.username || "Мастер"} />
                ) : (
                  <BriefcaseBusiness size={23} strokeWidth={2.3} />
                )}
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
  const currentTelegramId = telegramId()
  const [clients, setClients] = useState<MasterClient[]>([])
  const [loading, setLoading] = useState(true)
  const [query, setQuery] = useState("")
  const [addModalOpen, setAddModalOpen] = useState(false)
  const [clientSearch, setClientSearch] = useState("")
  const [savingClient, setSavingClient] = useState(false)
  const [message, setMessage] = useState("")

  function loadClients() {
    setLoading(true)
    fetch(`${API_URL}/api/master/${key}/clients`)
      .then((res) => res.json())
      .then((data) => setClients(Array.isArray(data) ? data : []))
      .catch((err) => console.error("Ошибка загрузки клиентов мастера:", err))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    loadClients()
  }, [key])

  function addClient() {
    const search = clientSearch.trim()

    if (!search) {
      setMessage("Введите username или телефон клиента")
      return
    }

    setSavingClient(true)
    setMessage("Добавляем клиента...")

    fetch(`${API_URL}/api/master/${key}/clients`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({ search }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось добавить клиента")
        }

        setMessage(data.message || "Клиент добавлен")
        setClientSearch("")
        setAddModalOpen(false)
        loadClients()
      })
      .catch((err) => setMessage(err.message || "Ошибка добавления клиента"))
      .finally(() => setSavingClient(false))
  }

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
        <p>База клиентов мастера и история посещений</p>
      </header>

      {message && <div className="profileMessage">{message}</div>}

      <section className="adminCard">
        <div className="clientsToolbar">
          <input
            className="adminInput searchInput"
            placeholder="Поиск по username или Telegram ID"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <button type="button" className="addClientButton" onClick={() => setAddModalOpen(true)} aria-label="Добавить клиента">
            <Plus size={22} strokeWidth={2.6} />
          </button>
        </div>
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

      {addModalOpen && (
        <div className="modalOverlay">
          <div className="modal profileSettingsModal">
            <h2>Добавить клиента</h2>
            <p className="modalHint">Введите Telegram username или телефон. Клиент должен хотя бы раз открыть бота.</p>
            <input
              className="adminInput"
              value={clientSearch}
              onChange={(event) => setClientSearch(event.target.value)}
              placeholder="@username или +79991234567"
              autoFocus
            />
            <div className="clientAddHint">
              <User size={19} strokeWidth={2.4} />
              <span>Если клиент не найден, отправьте ему ссылку на бота и попросите нажать Start.</span>
            </div>
            <div className="modalActions">
              <button type="button" className="cancelButton" onClick={() => setAddModalOpen(false)} disabled={savingClient}>
                Отмена
              </button>
              <button type="button" className="saveButton" onClick={addClient} disabled={savingClient}>
                {savingClient ? "Добавляем..." : "Добавить"}
              </button>
            </div>
          </div>
        </div>
      )}

      <MasterBottomNav masterKey={key ?? ""} />
    </main>
  )
}

function MasterBroadcastPage() {
  const { key } = useParams()
  const [mode, setMode] = useState<"all" | "personal">("all")
  const [title, setTitle] = useState("")
  const [text, setText] = useState("")
  const [message, setMessage] = useState("")
  const [sending, setSending] = useState(false)
  const [clients, setClients] = useState<MasterClient[]>([])
  const [clientsLoading, setClientsLoading] = useState(true)
  const [clientQuery, setClientQuery] = useState("")
  const [selectedClient, setSelectedClient] = useState<MasterClient | null>(null)

  useEffect(() => {
    fetch(`${API_URL}/api/master/${key}/clients`)
      .then((res) => res.json())
      .then((data) => setClients(Array.isArray(data) ? data : []))
      .catch(() => setClients([]))
      .finally(() => setClientsLoading(false))
  }, [key])

  const normalizedClientQuery = clientQuery.trim().toLowerCase()
  const filteredClients = clients.filter((client) => {
    const username = client.username?.toLowerCase() ?? ""
    const telegramIdText = String(client.telegramId)
    return username.includes(normalizedClientQuery) || telegramIdText.includes(normalizedClientQuery)
  })

  function sendBroadcast() {
    setMessage("")

    if (!title.trim()) {
      setMessage(mode === "personal" ? "Введите заголовок сообщения" : "Введите заголовок рассылки")
      return
    }

    if (!text.trim()) {
      setMessage(mode === "personal" ? "Введите текст сообщения" : "Введите основной текст")
      return
    }

    if (mode === "personal" && !selectedClient) {
      setMessage("Выберите клиента")
      return
    }

    setSending(true)

    const url = mode === "personal" && selectedClient
      ? `${API_URL}/api/master/${key}/broadcast/${selectedClient.telegramId}`
      : `${API_URL}/api/master/${key}/broadcast`

    fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": telegramId(),
      },
      body: JSON.stringify({
        title: title.trim(),
        text: text.trim(),
      }),
    })
      .then(async (res) => {
        const data = await res.json()

        if (!res.ok) {
          setMessage(data.message || "Не удалось отправить рассылку")
          return
        }

        setMessage(data.message || "Рассылка отправлена")
        setTitle("")
        setText("")
        setSelectedClient(null)
      })
      .catch(() => setMessage("Ошибка соединения с сервером"))
      .finally(() => setSending(false))
  }

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Рассылка</h1>
        <p>Отправьте сообщение клиентам, которые уже записывались к вам</p>
      </header>

      <section className="adminCard broadcastCard">
        <div className="scheduleModeSwitch">
          <button className={mode === "all" ? "active" : ""} onClick={() => setMode("all")}>
            Всем клиентам
          </button>
          <button className={mode === "personal" ? "active" : ""} onClick={() => setMode("personal")}>
            Персонально
          </button>
        </div>

        {mode === "personal" ? (
          <div className="addForm">
            <input
              className="adminInput searchInput"
              placeholder="Поиск клиента по username или Telegram ID"
              value={clientQuery}
              onChange={(event) => setClientQuery(event.target.value)}
            />

            <div className="broadcastClientList">
              {clientsLoading ? (
                <div className="emptyCard">Загружаем клиентов...</div>
              ) : filteredClients.length === 0 ? (
                <div className="emptyCard">Клиенты не найдены</div>
              ) : (
                filteredClients.map((client) => (
                  <button
                    className={`broadcastClientButton ${selectedClient?.telegramId === client.telegramId ? "active" : ""}`}
                    key={client.telegramId}
                    type="button"
                    onClick={() => setSelectedClient(client)}
                  >
                    <span className="masterAvatar">
                      <User size={22} strokeWidth={2.4} />
                    </span>
                    <span>
                      <strong>@{client.username || "unknown"}</strong>
                      <small>ID {client.telegramId} · записей {client.bookingsCount}</small>
                    </span>
                  </button>
                ))
              )}
            </div>

            {selectedClient && (
              <div className="editingNotice">
                <Send size={18} strokeWidth={2.4} />
                <span>Получатель: @{selectedClient.username || "unknown"}</span>
              </div>
            )}

            <input
              className="adminInput"
              placeholder="Заголовок"
              maxLength={80}
              value={title}
              onChange={(event) => setTitle(event.target.value)}
            />

            <textarea
              className="adminInput broadcastTextarea"
              placeholder="Текст личного сообщения"
              maxLength={900}
              value={text}
              onChange={(event) => setText(event.target.value)}
            />

            <div className="broadcastPreview">
              <span><Send size={20} strokeWidth={2.4} /></span>
              <div>
                <strong>{title.trim() || "Заголовок сообщения"}</strong>
                <p>{text.trim() || "Клиент получит это сообщение лично. Внизу будет кнопка на ваш публичный профиль."}</p>
              </div>
            </div>

            <button className="primaryButton iconButton" disabled={sending} onClick={sendBroadcast}>
              <Send size={18} strokeWidth={2.5} />
              {sending ? "Отправляем..." : "Отправить клиенту"}
            </button>
          </div>
        ) : (
          <div className="addForm">
            <input
              className="adminInput"
              placeholder="Заголовок"
              maxLength={80}
              value={title}
              onChange={(event) => setTitle(event.target.value)}
            />

            <textarea
              className="adminInput broadcastTextarea"
              placeholder="Основной текст сообщения"
              maxLength={900}
              value={text}
              onChange={(event) => setText(event.target.value)}
            />

            <div className="broadcastPreview">
              <span><Megaphone size={20} strokeWidth={2.4} /></span>
              <div>
                <strong>{title.trim() || "Заголовок рассылки"}</strong>
                <p>{text.trim() || "Тут будет основной текст. В сообщении клиенту также будет кнопка на ваш публичный профиль."}</p>
              </div>
            </div>

            <button className="primaryButton iconButton" disabled={sending} onClick={sendBroadcast}>
              <Send size={18} strokeWidth={2.5} />
              {sending ? "Отправляем..." : "Отправить рассылку"}
            </button>
          </div>
        )}

        {message && <p className="formMessage">{message}</p>}
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
  const [isVariablePrice, setIsVariablePrice] = useState(false)
  const [maxPrice, setMaxPrice] = useState("")
  const [duration, setDuration] = useState("")
  const [prepaymentPercent, setPrepaymentPercent] = useState("0")
  const [addressId, setAddressId] = useState("")
  const [addresses, setAddresses] = useState<MasterAddress[]>([])
  const [message, setMessage] = useState("")
  const [loadError, setLoadError] = useState("")
  const [editingService, setEditingService] = useState<MasterServiceItem | null>(null)
  const [hasPaymentDetails, setHasPaymentDetails] = useState(false)

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

    fetch(`${API_URL}/api/master/${key}`)
      .then((res) => res.json())
      .then((data) => setHasPaymentDetails(Boolean(data.paymentDetails?.trim())))
      .catch(() => setHasPaymentDetails(false))

    fetch(`${API_URL}/api/master/${key}/addresses`)
      .then((res) => res.json())
      .then((data) => setAddresses(Array.isArray(data) ? data : []))
      .catch(() => setAddresses([]))
  }, [key])

  function resetServiceForm() {
    setName("")
    setPrice("")
    setIsVariablePrice(false)
    setMaxPrice("")
    setDuration("")
    setPrepaymentPercent("0")
    setAddressId("")
    setEditingService(null)
  }

  function startEditService(service: MasterServiceItem) {
    setEditingService(service)
    setName(service.name)
    setPrice(String(service.price))
    setIsVariablePrice(Boolean(service.isVariablePrice))
    setMaxPrice(service.maxPrice ? String(service.maxPrice) : "")
    setDuration(String(service.duration))
    setPrepaymentPercent(String(service.prepaymentPercent))
    setAddressId(service.addressId ? String(service.addressId) : "")
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
    const maxPriceValue = Number(maxPrice)
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

    if (isVariablePrice && (!Number.isInteger(maxPriceValue) || maxPriceValue <= priceValue)) {
      setMessage("Максимальная цена должна быть больше минимальной")
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

    if (prepaymentValue > 0 && !hasPaymentDetails) {
      setMessage("Сначала добавьте реквизиты в профиле мастера, потом можно включить предоплату")
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
        isVariablePrice,
        maxPrice: isVariablePrice ? maxPriceValue : null,
        duration: durationValue,
        prepaymentPercent: prepaymentValue,
        addressId: addressId ? Number(addressId) : null,
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

            <label className="fieldLabel" htmlFor="serviceAddress">
              Адрес услуги
            </label>
            <select
              id="serviceAddress"
              className="adminInput"
              value={addressId}
              onChange={(event) => setAddressId(event.target.value)}
            >
              <option value="">Без адреса</option>
              {addresses.map((address) => (
                <option value={address.id} key={address.id}>
                  {address.title} - {address.address}
                </option>
              ))}
            </select>
            {addresses.length === 0 && (
              <p className="formHint">Адреса можно добавить в профиле мастера.</p>
            )}

            <label className="settingsToggleRow">
              <input
                type="checkbox"
                checked={isVariablePrice}
                onChange={(event) => setIsVariablePrice(event.target.checked)}
              />
              <span>
                <strong>Вариативная цена</strong>
                <small>Показывать клиентам диапазон цены</small>
              </span>
            </label>

            {isVariablePrice && (
              <input
                className="adminInput"
                inputMode="numeric"
                placeholder="Максимальная цена"
                value={maxPrice}
                onChange={(e) => setMaxPrice(e.target.value)}
              />
            )}

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
                <ServiceMeta icon={<Banknote />} label="Цена" value={formatServicePrice(service)} />
                <ServiceMeta icon={<Clock />} label="Время" value={`${service.duration} мин`} />
                {service.address && <ServiceMeta icon={<MapPin />} label="Адрес" value={service.address} />}
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

function MasterProfilePage() {
  const { key } = useParams()
  const currentTelegramId = telegramId()
  const [master, setMaster] = useState<Master | null>(null)
  const [loading, setLoading] = useState(true)
  const [name, setName] = useState("")
  const [description, setDescription] = useState("")
  const [paymentDetails, setPaymentDetails] = useState("")
  const [phoneNumber, setPhoneNumber] = useState("")
  const [addresses, setAddresses] = useState<MasterAddress[]>([])
  const [addressTitle, setAddressTitle] = useState("")
  const [addressText, setAddressText] = useState("")
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [avatarPreview, setAvatarPreview] = useState("")
  const [avatarSourceUrl, setAvatarSourceUrl] = useState("")
  const [cropModalOpen, setCropModalOpen] = useState(false)
  const [cropScale, setCropScale] = useState(1)
  const [cropPosition, setCropPosition] = useState({ x: 0, y: 0 })
  const [cropImageSize, setCropImageSize] = useState({ width: 0, height: 0 })
  const [avatarSaving, setAvatarSaving] = useState(false)
  const [portfolioPhotos, setPortfolioPhotos] = useState<PortfolioPhoto[]>([])
  const [portfolioSaving, setPortfolioSaving] = useState(false)
  const [portfolioViewerIndex, setPortfolioViewerIndex] = useState<number | null>(null)
  const [message, setMessage] = useState("")
  const [profileEditor, setProfileEditor] = useState<"phone" | "payment" | "addresses" | null>(null)
  const cropDrag = useRef<{ startX: number; startY: number; originX: number; originY: number } | null>(null)

  useEffect(() => {
    fetch(`${API_URL}/api/master/${key}`)
      .then(async (res) => {
        if (!res.ok) throw new Error("Мастер не найден")

        const data = await res.json()
        const normalizedMaster = normalizeMaster(data)
        setMaster(normalizedMaster)
        setName(normalizedMaster.name || "")
        setDescription(normalizedMaster.description || "")
        setPaymentDetails(normalizedMaster.paymentDetails || "")
        setPhoneNumber(normalizedMaster.phoneNumber || "")
      })
      .catch((err) => setMessage(err.message || "Не удалось загрузить профиль"))
      .finally(() => setLoading(false))
  }, [key])

  function loadAddresses() {
    fetch(`${API_URL}/api/master/${key}/addresses`)
      .then((res) => res.json())
      .then((data) => setAddresses(Array.isArray(data) ? data : []))
      .catch(() => setAddresses([]))
  }

  function loadPortfolio() {
    if (!key) return

    fetch(`${API_URL}/api/master/${key}/portfolio`)
      .then((res) => res.json())
      .then((data) => setPortfolioPhotos(Array.isArray(data) ? data.map(normalizePortfolioPhoto) : []))
      .catch(() => setPortfolioPhotos([]))
  }

  useEffect(() => {
    loadAddresses()
    loadPortfolio()
  }, [key])

  useEffect(() => {
    return () => {
      if (avatarPreview) URL.revokeObjectURL(avatarPreview)
    }
  }, [avatarPreview])

  useEffect(() => {
    return () => {
      if (avatarSourceUrl) URL.revokeObjectURL(avatarSourceUrl)
    }
  }, [avatarSourceUrl])

  function selectAvatar(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]

    if (!file) return

    if (!file.type.startsWith("image/")) {
      setMessage("Выберите файл картинки")
      event.target.value = ""
      return
    }

    const sourceUrl = URL.createObjectURL(file)

    setAvatarSourceUrl(sourceUrl)
    setAvatarFile(null)
    setAvatarPreview("")
    setCropScale(1)
    setCropPosition({ x: 0, y: 0 })
    setCropImageSize({ width: 0, height: 0 })
    setCropModalOpen(true)
    setMessage("")
    event.target.value = ""
  }

  function resetAvatarPreview() {
    setAvatarFile(null)
    setAvatarPreview("")
    setAvatarSourceUrl("")
    setCropModalOpen(false)
    setCropScale(1)
    setCropPosition({ x: 0, y: 0 })
    setCropImageSize({ width: 0, height: 0 })
  }

  function handleCropPointerDown(event: PointerEvent<HTMLDivElement>) {
    cropDrag.current = {
      startX: event.clientX,
      startY: event.clientY,
      originX: cropPosition.x,
      originY: cropPosition.y,
    }
    event.currentTarget.setPointerCapture(event.pointerId)
  }

  function handleCropPointerMove(event: PointerEvent<HTMLDivElement>) {
    if (!cropDrag.current) return

    setCropPosition({
      x: cropDrag.current.originX + event.clientX - cropDrag.current.startX,
      y: cropDrag.current.originY + event.clientY - cropDrag.current.startY,
    })
  }

  function handleCropPointerEnd() {
    cropDrag.current = null
  }

  function loadCropImage(src: string) {
    return new Promise<HTMLImageElement>((resolve, reject) => {
      const image = new Image()
      image.onload = () => resolve(image)
      image.onerror = reject
      image.src = src
    })
  }

  async function applyAvatarCrop() {
    if (!avatarSourceUrl) return

    const image = await loadCropImage(avatarSourceUrl)
    const viewportSize = 260
    const outputSize = 800
    const outputRatio = outputSize / viewportSize
    const baseScale = Math.max(viewportSize / image.naturalWidth, viewportSize / image.naturalHeight)
    const canvas = document.createElement("canvas")
    const context = canvas.getContext("2d")

    if (!context) {
      setMessage("Не удалось подготовить фото")
      return
    }

    canvas.width = outputSize
    canvas.height = outputSize
    context.fillStyle = "#ffffff"
    context.fillRect(0, 0, outputSize, outputSize)
    context.translate(outputSize / 2 + cropPosition.x * outputRatio, outputSize / 2 + cropPosition.y * outputRatio)
    context.scale(baseScale * cropScale * outputRatio, baseScale * cropScale * outputRatio)
    context.drawImage(image, -image.naturalWidth / 2, -image.naturalHeight / 2)

    canvas.toBlob((blob) => {
      if (!blob) {
        setMessage("Не удалось подготовить фото")
        return
      }

      const file = new File([blob], "master-avatar.jpg", { type: "image/jpeg" })
      const previewUrl = URL.createObjectURL(blob)

      setAvatarFile(file)
      setAvatarPreview(previewUrl)
      setCropModalOpen(false)
      setMessage("")
    }, "image/jpeg", 0.92)
  }

  const cropViewportSize = 260
  const cropBaseScale = cropImageSize.width && cropImageSize.height
    ? Math.max(cropViewportSize / cropImageSize.width, cropViewportSize / cropImageSize.height)
    : 1

  function saveAvatar() {
    if (!avatarFile || !master) return

    const formData = new FormData()
    formData.append("file", avatarFile)

    setAvatarSaving(true)
    setMessage("Загружаем фото...")

    fetch(`${API_URL}/api/master/${key}/avatar`, {
      method: "POST",
      headers: {
        "X-Telegram-Id": currentTelegramId,
      },
      body: formData,
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось загрузить фото")
        }

        setMaster({
          ...master,
          avatarUrl: data.avatarUrl || master.avatarUrl,
        })
        setAvatarFile(null)
        setAvatarPreview("")
        setMessage(data.message || "Фото профиля обновлено")
      })
      .catch((err) => setMessage(err.message || "Ошибка загрузки фото"))
      .finally(() => setAvatarSaving(false))
  }

  function uploadPortfolioPhoto(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ""

    if (!file || !key) return

    if (!file.type.startsWith("image/")) {
      setMessage("Выберите файл картинки")
      return
    }

    if (portfolioPhotos.length >= 9) {
      setMessage("Можно загрузить максимум 9 фото")
      return
    }

    const formData = new FormData()
    formData.append("file", file)

    setPortfolioSaving(true)
    setMessage("Загружаем фото в портфолио...")

    fetch(`${API_URL}/api/master/${key}/portfolio`, {
      method: "POST",
      headers: { "X-Telegram-Id": currentTelegramId },
      body: formData,
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось загрузить фото")
        }

        const photo = data?.photo ? normalizePortfolioPhoto(data.photo) : null

        if (photo?.id) {
          setPortfolioPhotos((photos) => [...photos, photo].sort((a, b) => a.sortOrder - b.sortOrder))
        } else {
          loadPortfolio()
        }

        setMessage(data?.message || "Фото добавлено")
      })
      .catch((err) => setMessage(err.message || "Ошибка загрузки фото"))
      .finally(() => setPortfolioSaving(false))
  }

  function deletePortfolioPhoto(photoId: number) {
    if (!key) return

    setPortfolioSaving(true)
    setMessage("Удаляем фото...")

    fetch(`${API_URL}/api/master/${key}/portfolio/${photoId}`, {
      method: "DELETE",
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось удалить фото")
        }

        setPortfolioPhotos((photos) =>
          photos
            .filter((photo) => photo.id !== photoId)
            .map((photo, index) => ({ ...photo, sortOrder: index }))
        )
        setPortfolioViewerIndex(null)
        setMessage(data?.message || "Фото удалено")
      })
      .catch((err) => setMessage(err.message || "Ошибка удаления фото"))
      .finally(() => setPortfolioSaving(false))
  }

  function savePortfolioOrder(nextPhotos: PortfolioPhoto[]) {
    if (!key) return

    setPortfolioSaving(true)

    fetch(`${API_URL}/api/master/${key}/portfolio/reorder`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({
        photoIds: nextPhotos.map((photo) => photo.id),
      }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось сохранить порядок")
        }

        setMessage(data?.message || "Порядок фото обновлен")
      })
      .catch((err) => {
        setMessage(err.message || "Ошибка изменения порядка")
        loadPortfolio()
      })
      .finally(() => setPortfolioSaving(false))
  }

  function movePortfolioPhoto(index: number, direction: -1 | 1) {
    const nextIndex = index + direction

    if (nextIndex < 0 || nextIndex >= portfolioPhotos.length) return

    const nextPhotos = portfolioPhotos.slice()
    const current = nextPhotos[index]
    nextPhotos[index] = nextPhotos[nextIndex]
    nextPhotos[nextIndex] = current

    const normalized = nextPhotos.map((photo, order) => ({ ...photo, sortOrder: order }))
    setPortfolioPhotos(normalized)
    savePortfolioOrder(normalized)
  }

  function addAddress() {
    if (!addressTitle.trim() || !addressText.trim()) {
      setMessage("Заполните название и адрес")
      return
    }

    setMessage("Добавляем адрес...")

    fetch(`${API_URL}/api/master/${key}/addresses`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({
        title: addressTitle.trim(),
        address: addressText.trim(),
      }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось добавить адрес")
        }

        setAddressTitle("")
        setAddressText("")
        setMessage(data.message || "Адрес добавлен")
        loadAddresses()
      })
      .catch((err) => setMessage(err.message || "Ошибка добавления адреса"))
  }

  function deleteAddress(addressId: number) {
    setMessage("Удаляем адрес...")

    fetch(`${API_URL}/api/master/${key}/addresses/${addressId}`, {
      method: "DELETE",
      headers: { "X-Telegram-Id": currentTelegramId },
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось удалить адрес")
        }

        setMessage(data.message || "Адрес удалён")
        loadAddresses()
      })
      .catch((err) => setMessage(err.message || "Ошибка удаления адреса"))
  }
  function saveProfile() {
    if (!master) return

    setMessage("Сохраняем...")

    fetch(`${API_URL}/api/master/${key}/profile`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "X-Telegram-Id": currentTelegramId,
      },
      body: JSON.stringify({
        name,
        description,
        paymentDetails,
        phoneNumber,
      }),
    })
      .then(async (res) => {
        const data = await res.json().catch(() => null)

        if (!res.ok || data?.success === false) {
          throw new Error(data?.message || "Не удалось сохранить профиль")
        }

        setMaster({
          ...master,
          name: name.trim(),
          description: description.trim(),
          paymentDetails: paymentDetails.trim(),
          phoneNumber: phoneNumber.trim(),
        })
        setProfileEditor(null)
        setMessage("Профиль сохранён")
      })
      .catch((err) => setMessage(err.message || "Ошибка сохранения"))
  }

  const selectedPortfolioPhoto = portfolioViewerIndex === null ? null : portfolioPhotos[portfolioViewerIndex] ?? null

  return (
    <main className="app">
      <header className="adminHeader">
        <h1>Профиль</h1>
        <p>Основные данные мастера и реквизиты для предоплаты</p>
      </header>

      {message && <div className="profileMessage">{message}</div>}

      <section className="adminCard serviceFormCard">
        {loading ? (
          <div className="emptyLine">Загружаем профиль...</div>
        ) : !master ? (
          <div className="emptyLine">Профиль не найден</div>
        ) : (
          <div className="addForm">
            <div className="avatarEditor">
              <div className="avatarPreview">
                {avatarPreview || master.avatarUrl ? (
                  <img src={avatarPreview || master.avatarUrl} alt={name || "Фото мастера"} />
                ) : (
                  <Camera size={34} strokeWidth={2.2} />
                )}
              </div>
              <div className="avatarEditorText">
                <strong>Фото профиля</strong>
                <small>Картинка будет показана клиентам круглой аватаркой.</small>
                <div className="avatarActions">
                  <label className="avatarPickButton">
                    Выбрать фото
                    <input type="file" accept="image/jpeg,image/png,image/webp" onChange={selectAvatar} />
                  </label>
                  {avatarFile && (
                    <>
                      <button type="button" className="avatarSaveButton" onClick={saveAvatar} disabled={avatarSaving}>
                        {avatarSaving ? "Сохраняем..." : "Сохранить"}
                      </button>
                      <button type="button" className="avatarCancelButton" onClick={resetAvatarPreview} disabled={avatarSaving}>
                        Отмена
                      </button>
                    </>
                  )}
                </div>
              </div>
            </div>

            <div className="portfolioEditor">
              <div className="portfolioEditorHeader">
                <div>
                  <strong>Портфолио работ</strong>
                  <small>{portfolioPhotos.length} / 9 фото</small>
                </div>
                <label className={`portfolioUploadButton ${portfolioPhotos.length >= 9 || portfolioSaving ? "disabled" : ""}`}>
                  <Plus size={17} strokeWidth={2.4} />
                  Добавить
                  <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    disabled={portfolioPhotos.length >= 9 || portfolioSaving}
                    onChange={uploadPortfolioPhoto}
                  />
                </label>
              </div>

              {portfolioPhotos.length === 0 ? (
                <div className="portfolioEmpty">
                  <ImageIcon size={28} strokeWidth={2.2} />
                  <span>Добавьте до 9 фото работ. Они появятся в публичном профиле мастера.</span>
                </div>
              ) : (
                <div className="portfolioEditorGrid">
                  {portfolioPhotos.map((photo, index) => (
                    <div className="portfolioEditorTile" key={photo.id}>
                      <button
                        type="button"
                        className="portfolioImageButton"
                        onClick={() => setPortfolioViewerIndex(index)}
                        aria-label="Открыть фото"
                      >
                        <img src={photo.imageUrl} alt={`Работа ${index + 1}`} />
                        <span>
                          <Maximize2 size={16} strokeWidth={2.4} />
                        </span>
                      </button>
                      <div className="portfolioTileActions">
                        <button
                          type="button"
                          disabled={index === 0 || portfolioSaving}
                          onClick={() => movePortfolioPhoto(index, -1)}
                          aria-label="Переместить левее"
                        >
                          <ArrowLeft size={15} strokeWidth={2.4} />
                        </button>
                        <button
                          type="button"
                          disabled={index === portfolioPhotos.length - 1 || portfolioSaving}
                          onClick={() => movePortfolioPhoto(index, 1)}
                          aria-label="Переместить правее"
                        >
                          <ArrowRight size={15} strokeWidth={2.4} />
                        </button>
                        <button
                          type="button"
                          className="portfolioDeleteButton"
                          disabled={portfolioSaving}
                          onClick={() => deletePortfolioPhoto(photo.id)}
                          aria-label="Удалить фото"
                        >
                          <Trash2 size={15} strokeWidth={2.4} />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <label className="fieldLabel" htmlFor="masterName">Имя в публичном профиле</label>
            <input
              id="masterName"
              className="adminInput"
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Например: Анна Смирнова"
            />

            <label className="fieldLabel" htmlFor="masterDescription">Описание</label>
            <textarea
              id="masterDescription"
              className="adminInput broadcastTextarea"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              placeholder="Расскажите клиентам о себе"
            />

            <div className="profileSettingsList">
              <button type="button" className="profileSettingsCard" onClick={() => setProfileEditor("phone")}>
                <span className="profileSettingsIcon">
                  <Phone size={22} strokeWidth={2.4} />
                </span>
                <span>
                  <strong>Телефон</strong>
                  <small>{phoneNumber.trim() || "Не указан"}</small>
                </span>
                <ChevronRight size={22} strokeWidth={2.4} />
              </button>

              <button type="button" className="profileSettingsCard" onClick={() => setProfileEditor("payment")}>
                <span className="profileSettingsIcon">
                  <CreditCard size={22} strokeWidth={2.4} />
                </span>
                <span>
                  <strong>Реквизиты</strong>
                  <small>{paymentDetails.trim() ? "Заполнены" : "Нужны для услуг с предоплатой"}</small>
                </span>
                <ChevronRight size={22} strokeWidth={2.4} />
              </button>

              <button type="button" className="profileSettingsCard" onClick={() => setProfileEditor("addresses")}>
                <span className="profileSettingsIcon">
                  <MapPin size={22} strokeWidth={2.4} />
                </span>
                <span>
                  <strong>Адреса</strong>
                  <small>{addresses.length > 0 ? `${addresses.length} адрес(а)` : "Добавьте места приёма"}</small>
                </span>
                <ChevronRight size={22} strokeWidth={2.4} />
              </button>
            </div>

            <label className="fieldLabel legacyProfileField" htmlFor="masterPhone">Телефон мастера</label>
            <input
              id="masterPhone"
              className="adminInput"
              inputMode="tel"
              value={phoneNumber}
              onChange={(event) => setPhoneNumber(event.target.value)}
              placeholder="+7..."
            />

            <div className="paymentDetailsNotice">
              <CreditCard size={22} strokeWidth={2.4} />
              <div>
                <strong>Реквизиты для предоплаты</strong>
                <p>Если хотите делать услуги с предоплатой, сначала заполните это поле.</p>
              </div>
            </div>

            <textarea
              className="adminInput broadcastTextarea"
              value={paymentDetails}
              onChange={(event) => setPaymentDetails(event.target.value)}
              placeholder="Например: СБП +7..., банк, получатель"
            />

            <button className="primaryButton" type="button" onClick={saveProfile}>
              Сохранить профиль
            </button>
          </div>
        )}
      </section>

      {cropModalOpen && avatarSourceUrl && (
        <div className="modalOverlay">
          <div className="modal avatarCropModal">
            <h2>Настройте фото</h2>
            <p className="modalHint">Передвиньте фото так, чтобы в круге осталось главное.</p>

            <div
              className="avatarCropArea"
              onPointerDown={handleCropPointerDown}
              onPointerMove={handleCropPointerMove}
              onPointerUp={handleCropPointerEnd}
              onPointerCancel={handleCropPointerEnd}
            >
              <img
                className="avatarCropImage"
                src={avatarSourceUrl}
                alt="Предпросмотр фото"
                draggable={false}
                onLoad={(event) => {
                  setCropImageSize({
                    width: event.currentTarget.naturalWidth,
                    height: event.currentTarget.naturalHeight,
                  })
                }}
                style={{
                  width: cropImageSize.width ? `${cropImageSize.width * cropBaseScale}px` : undefined,
                  height: cropImageSize.height ? `${cropImageSize.height * cropBaseScale}px` : undefined,
                  transform: `translate(-50%, -50%) translate(${cropPosition.x}px, ${cropPosition.y}px) scale(${cropScale})`,
                }}
              />
              <div className="avatarCropMask" />
            </div>

            <label className="fieldLabel" htmlFor="avatarZoom">Масштаб</label>
            <input
              id="avatarZoom"
              className="avatarZoomInput"
              type="range"
              min="1"
              max="2.6"
              step="0.05"
              value={cropScale}
              onChange={(event) => setCropScale(Number(event.target.value))}
            />

            <div className="modalActions">
              <button type="button" className="cancelButton" onClick={resetAvatarPreview}>
                Отмена
              </button>
              <button type="button" className="saveButton" onClick={applyAvatarCrop}>
                Готово
              </button>
            </div>
          </div>
        </div>
      )}

      {profileEditor && (
        <div className="modalOverlay">
          <div className="modal profileSettingsModal">
            <h2>{profileEditor === "phone" ? "Телефон" : profileEditor === "addresses" ? "Адреса" : "Реквизиты"}</h2>
            {profileEditor === "phone" ? (
              <>
                <p className="modalHint">Этот номер будет виден клиентам в публичном профиле.</p>
                <input
                  className="adminInput"
                  inputMode="tel"
                  value={phoneNumber}
                  onChange={(event) => setPhoneNumber(event.target.value)}
                  placeholder="+7..."
                />
              </>
            ) : profileEditor === "addresses" ? (
              <>
                <p className="modalHint">Добавьте адреса, которые потом можно выбрать у услуги.</p>
                <div className="addressList">
                  {addresses.length === 0 ? (
                    <div className="emptyLine">Адреса пока не добавлены</div>
                  ) : (
                    addresses.map((item) => (
                      <div className="addressItem" key={item.id}>
                        <span>
                          <strong>{item.title}</strong>
                          <small>{item.address}</small>
                        </span>
                        <button type="button" aria-label="Удалить адрес" onClick={() => deleteAddress(item.id)}>
                          <Trash2 size={17} strokeWidth={2.4} />
                        </button>
                      </div>
                    ))
                  )}
                </div>
                <input
                  className="adminInput"
                  value={addressTitle}
                  onChange={(event) => setAddressTitle(event.target.value)}
                  placeholder="Название: салон, дом, кабинет"
                />
                <textarea
                  className="adminInput broadcastTextarea"
                  value={addressText}
                  onChange={(event) => setAddressText(event.target.value)}
                  placeholder="Полный адрес"
                />
                <button type="button" className="primaryButton" onClick={addAddress}>
                  Добавить адрес
                </button>
              </>            ) : (
              <>
                <p className="modalHint">Реквизиты нужны, если у услуги включена предоплата.</p>
                <textarea
                  className="adminInput broadcastTextarea"
                  value={paymentDetails}
                  onChange={(event) => setPaymentDetails(event.target.value)}
                  placeholder="Например: СБП +7..., банк, получатель"
                />
              </>
            )}
            <div className="modalActions">
              <button type="button" className="cancelButton" onClick={() => setProfileEditor(null)}>
                Отмена
              </button>
              <button type="button" className="saveButton" onClick={profileEditor === "addresses" ? () => setProfileEditor(null) : saveProfile}>
                Сохранить
              </button>
            </div>
          </div>
        </div>
      )}

      {selectedPortfolioPhoto && (
        <div className="portfolioViewerOverlay">
          <button
            type="button"
            className="portfolioViewerClose"
            onClick={() => setPortfolioViewerIndex(null)}
            aria-label="Закрыть просмотр"
          >
            <X size={24} strokeWidth={2.4} />
          </button>
          <button
            type="button"
            className="portfolioViewerNav portfolioViewerPrev"
            disabled={portfolioViewerIndex === 0}
            onClick={() => setPortfolioViewerIndex((index) => index === null ? index : Math.max(0, index - 1))}
            aria-label="Предыдущее фото"
          >
            <ArrowLeft size={22} strokeWidth={2.4} />
          </button>
          <img src={selectedPortfolioPhoto.imageUrl} alt="Работа мастера" />
          <button
            type="button"
            className="portfolioViewerNav portfolioViewerNext"
            disabled={portfolioViewerIndex === portfolioPhotos.length - 1}
            onClick={() => setPortfolioViewerIndex((index) => index === null ? index : Math.min(portfolioPhotos.length - 1, index + 1))}
            aria-label="Следующее фото"
          >
            <ArrowRight size={22} strokeWidth={2.4} />
          </button>
          <div className="portfolioViewerCounter">
            {(portfolioViewerIndex ?? 0) + 1} / {portfolioPhotos.length}
          </div>
        </div>
      )}

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

void MasterComingSoon

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
    blocked: "Занято",
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
      <Link to={`/master/${masterKey}/block-time`} className="bottomNavMain">
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
