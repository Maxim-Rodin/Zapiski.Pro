export const YANDEX_METRIKA_ID = 110942038
export const YANDEX_METRIKA_GOAL = "telegram_click"

declare global {
  interface Window {
    ym?: (...args: unknown[]) => void
    disableYaCounter110942038?: boolean
  }
}

export function trackTelegramClick(placement: string) {
  window.ym?.(YANDEX_METRIKA_ID, "reachGoal", YANDEX_METRIKA_GOAL, { placement })
}
