import { ArrowLeft, BriefcaseBusiness, Send, ShieldCheck } from "lucide-react"
import type { ReactNode } from "react"
import { legalDetails } from "./legal"

export function LegalPage({
  eyebrow,
  title,
  description,
  children,
}: {
  eyebrow: string
  title: string
  description: string
  children: ReactNode
}) {
  return (
    <main className="legalPage">
      <header className="legalTopbar">
        <a className="logo" href="/" aria-label="Zapisi Pro — на главную">
          <span><BriefcaseBusiness size={19} strokeWidth={2.5} /></span>
          <b>Zapisi.Pro</b>
        </a>
        <a className="legalBack" href="/"><ArrowLeft size={17} /> На главную</a>
      </header>

      <section className="legalHero">
        <span>{eyebrow}</span>
        <h1>{title}</h1>
        <p>{description}</p>
        <div className="legalMeta">
          <span>Редакция {legalDetails.policyVersion}</span>
          <span>Действует с {legalDetails.revisionDate}</span>
        </div>
      </section>

      <div className="legalLayout">
        <aside className="legalAside">
          <ShieldCheck size={24} />
          <strong>Коротко</strong>
          <p>Яндекс Метрика загружается только после добровольного согласия посетителя. Вебвизор отключён.</p>
          <a href={legalDetails.supportUrl} target="_blank" rel="noopener noreferrer">
            <Send size={16} /> Связаться с нами
          </a>
        </aside>

        <article className="legalDocument">{children}</article>
      </div>

      <footer className="legalFooter">
        <span>© 2026 Zapisi Pro</span>
        <div><a href="/privacy">Конфиденциальность</a><a href="/consent">Согласие на обработку</a></div>
      </footer>
    </main>
  )
}
