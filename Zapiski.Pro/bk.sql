--
-- PostgreSQL database dump
--

\restrict OXDFCQ3HMnKgh2BindqNTngUzePBUDctbncVanOFh6ANWscIXdyQoduuipF7saz

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

-- Started on 2026-05-02 12:22:32

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 4 (class 2615 OID 2200)
-- Name: public; Type: SCHEMA; Schema: -; Owner: pg_database_owner
--

CREATE SCHEMA public;


ALTER SCHEMA public OWNER TO pg_database_owner;

--
-- TOC entry 5023 (class 0 OID 0)
-- Dependencies: 4
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: pg_database_owner
--

COMMENT ON SCHEMA public IS 'standard public schema';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 222 (class 1259 OID 74015)
-- Name: Bookings; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Bookings" (
    "idBooking" integer CONSTRAINT "Bookings_idBookiing_not_null" NOT NULL,
    "MasterId" integer,
    "UserId" bigint,
    "Date" date,
    "Time" time without time zone,
    "ServiceId" integer,
    "Status" text DEFAULT 'ожидание'::text,
    "StartTime" time without time zone,
    "EndTime" time without time zone,
    CONSTRAINT valid_time CHECK (("EndTime" > "StartTime"))
);


ALTER TABLE public."Bookings" OWNER TO postgres;

--
-- TOC entry 226 (class 1259 OID 74052)
-- Name: Bookings_idBookiing_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Bookings" ALTER COLUMN "idBooking" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Bookings_idBookiing_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 229 (class 1259 OID 82123)
-- Name: MasterSchedule; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."MasterSchedule" (
    "Id" integer NOT NULL,
    "MasterId" integer NOT NULL,
    "DayOfWeek" integer NOT NULL,
    "StartTime" time without time zone NOT NULL,
    "EndTime" time without time zone NOT NULL,
    "IsActive" boolean DEFAULT false
);


ALTER TABLE public."MasterSchedule" OWNER TO postgres;

--
-- TOC entry 228 (class 1259 OID 82122)
-- Name: MasterSchedule_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."MasterSchedule_Id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."MasterSchedule_Id_seq" OWNER TO postgres;

--
-- TOC entry 5024 (class 0 OID 0)
-- Dependencies: 228
-- Name: MasterSchedule_Id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."MasterSchedule_Id_seq" OWNED BY public."MasterSchedule"."Id";


--
-- TOC entry 219 (class 1259 OID 73931)
-- Name: Masters; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Masters" (
    "idMaster" integer CONSTRAINT "Maters_idMaster_not_null" NOT NULL,
    "Name" text,
    "Key" text,
    "PhotoPath" text,
    "UserId" integer,
    "Description" text
);


ALTER TABLE public."Masters" OWNER TO postgres;

--
-- TOC entry 225 (class 1259 OID 74051)
-- Name: Masters_idMaster_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Masters" ALTER COLUMN "idMaster" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Masters_idMaster_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 220 (class 1259 OID 73998)
-- Name: Services; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Services" (
    "idService" integer NOT NULL,
    "MasterId" integer,
    "Name" text,
    "Price" integer,
    "Duration" integer
);


ALTER TABLE public."Services" OWNER TO postgres;

--
-- TOC entry 224 (class 1259 OID 74050)
-- Name: Services_idService_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Services" ALTER COLUMN "idService" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Services_idService_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 227 (class 1259 OID 74085)
-- Name: UserStates; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."UserStates" (
    "TelegramId" bigint NOT NULL,
    "State" text,
    "Data" text
);


ALTER TABLE public."UserStates" OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 74006)
-- Name: Users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Users" (
    "idUser" integer NOT NULL,
    "TelegrammId" bigint,
    "UserName" text,
    "Role" text DEFAULT 'client'::text
);


ALTER TABLE public."Users" OWNER TO postgres;

--
-- TOC entry 223 (class 1259 OID 74049)
-- Name: Users_idUser_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Users" ALTER COLUMN "idUser" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Users_idUser_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 4835 (class 2604 OID 82126)
-- Name: MasterSchedule Id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MasterSchedule" ALTER COLUMN "Id" SET DEFAULT nextval('public."MasterSchedule_Id_seq"'::regclass);


--
-- TOC entry 5010 (class 0 OID 74015)
-- Dependencies: 222
-- Data for Name: Bookings; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Bookings" ("idBooking", "MasterId", "UserId", "Date", "Time", "ServiceId", "Status", "StartTime", "EndTime") OVERRIDING SYSTEM VALUE VALUES (13, 6, 1, '2026-05-02', '13:00:00', 1, 'confirmed', NULL, NULL);
INSERT INTO public."Bookings" ("idBooking", "MasterId", "UserId", "Date", "Time", "ServiceId", "Status", "StartTime", "EndTime") OVERRIDING SYSTEM VALUE VALUES (14, 8, 1, '2026-05-07', '14:08:00', 12, 'confirmed', NULL, NULL);
INSERT INTO public."Bookings" ("idBooking", "MasterId", "UserId", "Date", "Time", "ServiceId", "Status", "StartTime", "EndTime") OVERRIDING SYSTEM VALUE VALUES (15, 10, 1, '2026-05-05', '09:00:00', 20, 'pending', NULL, NULL);


--
-- TOC entry 5017 (class 0 OID 82123)
-- Dependencies: 229
-- Data for Name: MasterSchedule; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (2, 6, 2, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (3, 6, 3, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (4, 6, 4, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (5, 6, 5, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (7, 6, 7, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (6, 6, 6, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (1, 6, 1, '07:00:00', '19:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (9, 10, 1, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (10, 10, 2, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (11, 10, 3, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (12, 10, 4, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (13, 10, 5, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (14, 10, 6, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (15, 10, 7, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (16, 7, 1, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (17, 7, 2, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (18, 7, 3, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (19, 7, 4, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (20, 7, 5, '09:00:00', '18:00:00', true);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (21, 7, 6, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (22, 7, 7, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (28, 8, 6, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (29, 8, 7, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (23, 8, 1, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (24, 8, 2, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (25, 8, 3, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (27, 8, 5, '09:00:00', '18:00:00', false);
INSERT INTO public."MasterSchedule" ("Id", "MasterId", "DayOfWeek", "StartTime", "EndTime", "IsActive") VALUES (26, 8, 4, '13:00:00', '20:00:00', true);


--
-- TOC entry 5007 (class 0 OID 73931)
-- Dependencies: 219
-- Data for Name: Masters; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Masters" ("idMaster", "Name", "Key", "PhotoPath", "UserId", "Description") OVERRIDING SYSTEM VALUE VALUES (7, NULL, 'sosalkin', NULL, 3, 'Минет - 5000 
В попу - 10000');
INSERT INTO public."Masters" ("idMaster", "Name", "Key", "PhotoPath", "UserId", "Description") OVERRIDING SYSTEM VALUE VALUES (6, 'София', 'sofia', NULL, 4, 'Всем привет');
INSERT INTO public."Masters" ("idMaster", "Name", "Key", "PhotoPath", "UserId", "Description") OVERRIDING SYSTEM VALUE VALUES (8, NULL, 'vika_rodina', NULL, 5, NULL);
INSERT INTO public."Masters" ("idMaster", "Name", "Key", "PhotoPath", "UserId", "Description") OVERRIDING SYSTEM VALUE VALUES (9, 'Satoshi', 'vozdyx2', NULL, 2, NULL);
INSERT INTO public."Masters" ("idMaster", "Name", "Key", "PhotoPath", "UserId", "Description") OVERRIDING SYSTEM VALUE VALUES (10, NULL, 'timerlano', NULL, 6, NULL);


--
-- TOC entry 5008 (class 0 OID 73998)
-- Dependencies: 220
-- Data for Name: Services; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (1, 6, 'Прическа база', 2000, 60);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (5, 6, 'Ресницы', 1000, 60);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (6, 7, 'Минет', 5000, 10);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (7, 7, 'Золотой дождь', 15000, 5);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (8, 7, 'Копро', 7000, 25);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (9, 7, 'Стриптиз', 25000, 15);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (10, 7, 'Наездница', 8000, 3);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (11, 7, 'Стрижка в трусах и лифчике от блондинки', 12000, 60);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (3, 6, 'Ногти', 4000, 180);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (2, 6, 'Макияж', 4000, 120);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (12, 8, '7777', 5, 68);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (13, 9, '🇷🇺Госуслуги🇷🇺', 2000, 0);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (14, 9, '💳ЛК БАНКОВ РФ💳', 6000, 0);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (15, 9, '🖨ДОКУМЕНТЫ🖨', 10000, 0);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (16, 9, '📲eSIM[RU]📲', 2000, 0);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (17, 9, '☎️IP-ТЕЛЕФОНИЯ☎️', 2000, 0);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (19, 10, 'Заңды мәселелерді шешу', 1200000, 45);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (20, 10, 'Вопросы об устойчивости лошади каллы', 20000, 30);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (21, 10, 'Көп балалы милф табу', 35000, 203);
INSERT INTO public."Services" ("idService", "MasterId", "Name", "Price", "Duration") OVERRIDING SYSTEM VALUE VALUES (22, 8, 'ШАШЛЫКИ', 1000, 45);


--
-- TOC entry 5015 (class 0 OID 74085)
-- Dependencies: 227
-- Data for Name: UserStates; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- TOC entry 5009 (class 0 OID 74006)
-- Dependencies: 221
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Users" ("idUser", "TelegrammId", "UserName", "Role") OVERRIDING SYSTEM VALUE VALUES (1, 883551560, 'B001OP32', 'admin');
INSERT INTO public."Users" ("idUser", "TelegrammId", "UserName", "Role") OVERRIDING SYSTEM VALUE VALUES (4, 881761714, 'ssppriite', 'master');
INSERT INTO public."Users" ("idUser", "TelegrammId", "UserName", "Role") OVERRIDING SYSTEM VALUE VALUES (3, 824688046, 'A1MP97', 'master');
INSERT INTO public."Users" ("idUser", "TelegrammId", "UserName", "Role") OVERRIDING SYSTEM VALUE VALUES (5, 1049063882, 'vikiffg', 'master');
INSERT INTO public."Users" ("idUser", "TelegrammId", "UserName", "Role") OVERRIDING SYSTEM VALUE VALUES (2, 5149953219, 'official0wner', 'master');
INSERT INTO public."Users" ("idUser", "TelegrammId", "UserName", "Role") OVERRIDING SYSTEM VALUE VALUES (6, 1037100552, 'oa007o', 'master');


--
-- TOC entry 5025 (class 0 OID 0)
-- Dependencies: 226
-- Name: Bookings_idBookiing_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Bookings_idBookiing_seq"', 15, true);


--
-- TOC entry 5026 (class 0 OID 0)
-- Dependencies: 228
-- Name: MasterSchedule_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."MasterSchedule_Id_seq"', 29, true);


--
-- TOC entry 5027 (class 0 OID 0)
-- Dependencies: 225
-- Name: Masters_idMaster_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Masters_idMaster_seq"', 11, true);


--
-- TOC entry 5028 (class 0 OID 0)
-- Dependencies: 224
-- Name: Services_idService_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Services_idService_seq"', 22, true);


--
-- TOC entry 5029 (class 0 OID 0)
-- Dependencies: 223
-- Name: Users_idUser_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Users_idUser_seq"', 6, true);


--
-- TOC entry 4847 (class 2606 OID 74023)
-- Name: Bookings Bookings_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Bookings"
    ADD CONSTRAINT "Bookings_pkey" PRIMARY KEY ("idBooking");


--
-- TOC entry 4852 (class 2606 OID 82134)
-- Name: MasterSchedule MasterSchedule_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MasterSchedule"
    ADD CONSTRAINT "MasterSchedule_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4839 (class 2606 OID 73938)
-- Name: Masters Maters_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Masters"
    ADD CONSTRAINT "Maters_pkey" PRIMARY KEY ("idMaster");


--
-- TOC entry 4841 (class 2606 OID 74005)
-- Name: Services Services_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Services"
    ADD CONSTRAINT "Services_pkey" PRIMARY KEY ("idService");


--
-- TOC entry 4850 (class 2606 OID 74092)
-- Name: UserStates UserStates_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."UserStates"
    ADD CONSTRAINT "UserStates_pkey" PRIMARY KEY ("TelegramId");


--
-- TOC entry 4843 (class 2606 OID 74014)
-- Name: Users Users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_pkey" PRIMARY KEY ("idUser");


--
-- TOC entry 4845 (class 2606 OID 74084)
-- Name: Users uq_users_telegram; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT uq_users_telegram UNIQUE ("TelegrammId");


--
-- TOC entry 4848 (class 1259 OID 82140)
-- Name: idx_booking_master_date; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_booking_master_date ON public."Bookings" USING btree ("MasterId", "Date");


--
-- TOC entry 4855 (class 2606 OID 74034)
-- Name: Bookings Bookings_MasterId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Bookings"
    ADD CONSTRAINT "Bookings_MasterId_fkey" FOREIGN KEY ("MasterId") REFERENCES public."Masters"("idMaster") NOT VALID;


--
-- TOC entry 4856 (class 2606 OID 74044)
-- Name: Bookings Bookings_ServiceId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Bookings"
    ADD CONSTRAINT "Bookings_ServiceId_fkey" FOREIGN KEY ("ServiceId") REFERENCES public."Services"("idService") NOT VALID;


--
-- TOC entry 4857 (class 2606 OID 74039)
-- Name: Bookings Bookings_UserId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Bookings"
    ADD CONSTRAINT "Bookings_UserId_fkey" FOREIGN KEY ("UserId") REFERENCES public."Users"("idUser") NOT VALID;


--
-- TOC entry 4853 (class 2606 OID 74024)
-- Name: Masters Masters_UserId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Masters"
    ADD CONSTRAINT "Masters_UserId_fkey" FOREIGN KEY ("UserId") REFERENCES public."Users"("idUser") NOT VALID;


--
-- TOC entry 4854 (class 2606 OID 74029)
-- Name: Services Services_MasterId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Services"
    ADD CONSTRAINT "Services_MasterId_fkey" FOREIGN KEY ("MasterId") REFERENCES public."Masters"("idMaster") NOT VALID;


--
-- TOC entry 4859 (class 2606 OID 82135)
-- Name: MasterSchedule fk_master; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MasterSchedule"
    ADD CONSTRAINT fk_master FOREIGN KEY ("MasterId") REFERENCES public."Masters"("idMaster") ON DELETE CASCADE;


--
-- TOC entry 4858 (class 2606 OID 74093)
-- Name: UserStates fk_states_users; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."UserStates"
    ADD CONSTRAINT fk_states_users FOREIGN KEY ("TelegramId") REFERENCES public."Users"("TelegrammId") ON UPDATE CASCADE ON DELETE CASCADE;


-- Completed on 2026-05-02 12:22:32

--
-- PostgreSQL database dump complete
--

\unrestrict OXDFCQ3HMnKgh2BindqNTngUzePBUDctbncVanOFh6ANWscIXdyQoduuipF7saz

