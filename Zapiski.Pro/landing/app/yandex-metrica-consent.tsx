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
    setOpen(saved !== "accepted")
    if (saved === "accepted") loadMetrika()
  }, [])

  useEffect(() => {
    if (!open) return
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = "hidden"
    return () => {
      document.body.style.overflow = previousOverflow
    }
  }, [open])

  const accept = useCallback(() => {
    localStorage.setItem(CONSENT_KEY, "accepted")
    setConsent("accepted")
    setOpen(false)
    loadMetrika()
  }, [])

  const leaveSite = useCallback(() => {
    localStorage.setItem(CONSENT_KEY, "rejected")
    window.disableYaCounter110942038 = true
    removeMetrikaCookies()
    setConsent("rejected")
    const referrer = document.referrer
    const destination = referrer && new URL(referrer).origin !== location.origin
      ? referrer
      : "https://yandex.ru"
    location.replace(destination)
  }, [])

  if (!open && !consent) return null

  return (
    <>
      {open && (
        <>
          <div className="analytics-consent-backdrop" aria-hidden="true" />
          <aside className="analytics-consent" role="dialog" aria-modal="true" aria-label="Согласие на использование cookies">
            <div>
              <strong>Для работы сайта нужны cookies</strong>
              <p>
                Мы используем Яндекс Метрику, чтобы видеть посещения и переходы в Telegram.
                Продолжая, вы соглашаетесь на использование аналитических cookies.
                Вебвизор отключён. Подробнее — в <a href="/privacy">политике конфиденциальности</a>.
              </p>
            </div>
            <div className="analytics-actions">
              <button type="button" className="analytics-reject" onClick={leaveSite}>Покинуть сайт</button>
              <button type="button" className="analytics-accept" onClick={accept}>Принять и продолжить</button>
            </div>
          </aside>
        </>
      )}
      {!open && (
        <button className="analytics-settings" type="button" onClick={() => setOpen(true)}>
          Настройки аналитики
        </button>
      )}
    </>
  )
}
