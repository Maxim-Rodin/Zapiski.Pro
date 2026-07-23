"use client"

import { useCallback, useEffect, useState } from "react"
import { YANDEX_METRIKA_ID } from "@/lib/metrika"

const CONSENT_KEY = "zapisi_analytics_consent"
type Consent = "accepted" | "rejected" | null

function loadMetrika() {
  if (document.querySelector(`script[data-yandex-metrika="${YANDEX_METRIKA_ID}"]`)) return

  window.disableYaCounter110942038 = false
  window.ym = window.ym || function (...args: unknown[]) {
    ;(window.ym as unknown as { a?: unknown[] }).a =
      (window.ym as unknown as { a?: unknown[] }).a || []
    ;(window.ym as unknown as { a: unknown[] }).a.push(args)
  }

  window.ym(YANDEX_METRIKA_ID, "init", {
    accurateTrackBounce: true,
    clickmap: false,
    defer: false,
    trackLinks: true,
    webvisor: false,
  })

  const script = document.createElement("script")
  script.async = true
  script.dataset.yandexMetrika = String(YANDEX_METRIKA_ID)
  script.src = `https://mc.yandex.ru/metrika/tag.js?id=${YANDEX_METRIKA_ID}`
  document.head.appendChild(script)
}

function removeMetrikaCookies() {
  const names = document.cookie.split(";").map((cookie) => cookie.split("=")[0].trim())
  const domains = [location.hostname, `.${location.hostname}`, ".yandex.ru", ".yandex.com"]

  for (const name of names) {
    if (!name.startsWith("_ym") && name !== "yandexuid") continue
    document.cookie = `${name}=; Max-Age=0; path=/`
    for (const domain of domains) {
      document.cookie = `${name}=; Max-Age=0; path=/; domain=${domain}`
    }
  }
}

export function YandexMetrikaConsent() {
  const [consent, setConsent] = useState<Consent>(null)
  const [open, setOpen] = useState(false)

  useEffect(() => {
    const saved = localStorage.getItem(CONSENT_KEY) as Consent
    setConsent(saved)
    setOpen(saved !== "accepted" && saved !== "rejected")
    if (saved === "accepted") loadMetrika()
  }, [])

  const accept = useCallback(() => {
    localStorage.setItem(CONSENT_KEY, "accepted")
    setConsent("accepted")
    setOpen(false)
    loadMetrika()
  }, [])

  const reject = useCallback(() => {
    localStorage.setItem(CONSENT_KEY, "rejected")
    window.disableYaCounter110942038 = true
    removeMetrikaCookies()
    setConsent("rejected")
    setOpen(false)
  }, [])

  if (!open && !consent) return null

  return (
    <>
      {open && (
        <aside className="analytics-consent" role="dialog" aria-label="Настройки аналитики">
          <div>
            <strong>Помогите нам улучшать Zapisi Pro</strong>
            <p>
              С вашего согласия мы включим Яндекс Метрику, чтобы считать переходы в Telegram.
              Вебвизор отключён. Подробнее — в <a href="/privacy">политике конфиденциальности</a>.
            </p>
          </div>
          <div className="analytics-actions">
            <button type="button" className="analytics-reject" onClick={reject}>Только необходимые</button>
            <button type="button" className="analytics-accept" onClick={accept}>Разрешить аналитику</button>
          </div>
        </aside>
      )}
      {!open && (
        <button className="analytics-settings" type="button" onClick={() => setOpen(true)}>
          Настройки аналитики
        </button>
      )}
    </>
  )
}
