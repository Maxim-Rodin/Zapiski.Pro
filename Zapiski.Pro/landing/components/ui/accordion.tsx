"use client"

import * as AccordionPrimitive from "@radix-ui/react-accordion"
import { ChevronDown } from "lucide-react"

export const Accordion = AccordionPrimitive.Root

export function AccordionItem({ value, children }: { value: string; children: React.ReactNode }) {
  return <AccordionPrimitive.Item value={value} className="faq-item">{children}</AccordionPrimitive.Item>
}

export function AccordionTrigger({ children }: { children: React.ReactNode }) {
  return (
    <AccordionPrimitive.Header>
      <AccordionPrimitive.Trigger className="faq-trigger">
        {children}<ChevronDown size={20} className="faq-chevron" />
      </AccordionPrimitive.Trigger>
    </AccordionPrimitive.Header>
  )
}

export function AccordionContent({ children }: { children: React.ReactNode }) {
  return <AccordionPrimitive.Content className="faq-content"><div>{children}</div></AccordionPrimitive.Content>
}
