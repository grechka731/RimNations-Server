# RimNations: Dedicated Server Engine

Выделенный серверный комплекс для сетевой многопользовательской синхронизации мода RimNations (RimWorld 1.6).

[![Runtime](https://img.shields.io/badge/Runtime-Mono%206.x%20%7C%20.NET%20Framework%204.7.2-blue.svg)](https://www.mono-project.com/)
[![Platform](https://img.shields.io/badge/OS-Debian%2013%20%7C%20Ubuntu%20%7C%20Windows-blue.svg)](https://debian.org)
[![Code Origin](https://img.shields.io/badge/Codebase-Synthetic%20%2F%20AI%20Generated-purple.svg)](#происхождение-кодовой-базы-и-методология)
[![Latest Release](https://img.shields.io/github/v/release/grechka731/RimNations-Server?label=Release)](https://github.com/grechka731/RimNations-Server/releases/latest)

---

## Бинарные сборки (Releases)

Автономные исполняемые сборки сервера, не требующие ручной компиляции, публикуются в разделе релизов:
- **Официальный релиз:** [GitHub Releases v1.0.0](https://github.com/grechka731/RimNations-Server/releases/latest)
- **Прямая загрузка пакета:** [`RimNations-Dedicated-Server-v1.0.0.zip`](https://github.com/grechka731/RimNations-Server/releases/download/v1.0.0/RimNations-Dedicated-Server-v1.0.0.zip)

---

## Системное развертывание

### Развертывание на Debian 13 / Ubuntu (Systemd)

1. Установка среды выполнения Mono:
```bash
sudo apt update
sudo apt install -y mono-complete unzip
```

2. Развертывание пакета сервера:
```bash
sudo mkdir -p /opt/rimnations
sudo unzip RimNations-Dedicated-Server-v1.0.0.zip -d /opt/rimnations
sudo chmod +x /opt/rimnations/start_server.sh
```

3. Регистрация системной службы:
```bash
sudo cp /opt/rimnations/rimnations.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable rimnations
sudo systemctl start rimnations
```

4. Мониторинг журнала:
```bash
journalctl -u rimnations -f
```

### Запуск на Windows
1. Распаковать архив `RimNations-Dedicated-Server-v1.0.0.zip`.
2. Выполнить запуск файла `StartServer.bat` (или напрямую `Server.exe`).
3. По умолчанию сетевой сокет прослушивает TCP-порт `19019`.

---

## Архитектура сервера

- **Сетевой транспорт:** Асинхронный сокет TCP с оптимизированной бинарной сериализацией (`OCUnion`).
- **Синхронизация состояния:** Централизованное версионирование снимков мира (`WorldObjectRepository`) с передачей инкрементальной дельты данных.
- **Поддержка сетевых топологий:** Поддержка проксирования через reverse-tunnel / VPN (WireGuard, GRE, Cloudflare Spectrum) для изоляции белого IP-адреса и фильтрации трафика от DDoS-атак.

---

## Происхождение кодовой базы и методология

Данный репозиторий представляет собой результат реализации парадигмы автоматизированной генерации программного обеспечения методом сквозного ИИ-программирования («вайбкодинг»).

1. **Генерация:** 100% серверного кода, сетевых интерфейсов, бинарных упаковщиков и процедур персистентности сгенерированы искусственным интеллектом (Google Antigravity AI).
2. **Верификация:** Компиляция завершена со статусом 0 ошибок на стеке Mono 6.x и .NET Framework 4.7.2.
3. **Характер кода:** Код является полностью синтетическим. Архитектура оптимизирована под практическую стабильность при высокочастотной нагрузке сетевых сессий.

---

## Сборка из исходных кодов

```bash
cd Source
dotnet build Server.sln -c Release -p:RimWorldFolder="<Path_To_RimWorld>"
```

Исполняемый файл компилируется в:
`BuildOutput/Server/Release/Server.exe`
