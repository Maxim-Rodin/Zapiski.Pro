import type { Metadata } from "next"
import "./globals.css"

export const metadata: Metadata = {
  title: "Zapisi Pro — онлайн-запись внутри Telegram",
  description: "Клиенты выбирают услугу и свободное время сами. Вы управляете расписанием прямо в Telegram.",
}

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="ru"><body>{children}</body></html>
}
