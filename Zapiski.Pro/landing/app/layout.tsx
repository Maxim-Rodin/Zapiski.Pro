import type { Metadata } from "next"
import "./globals.css"
import { YandexMetrikaConsent } from "./yandex-metrica-consent"

export const metadata: Metadata = {
  title: "Zapisi Pro — онлайн-запись внутри Telegram",
  description: "Клиенты выбирают услугу и свободное время сами. Вы управляете расписанием прямо в Telegram.",
}

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="ru">
      <body>
        {children}
        <YandexMetrikaConsent />
      </body>
    </html>
  )
}
