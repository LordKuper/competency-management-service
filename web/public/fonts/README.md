# Шрифты PT Sans, PT Sans Narrow, PT Mono

Шрифты раздаются с того же origin, из CDN ничего не подключается. Лицензия — SIL Open Font License 1.1: текст и уведомление об авторских правах лежат рядом (`OFL-PTSans.txt` — PT Sans и PT Sans Narrow, `OFL-PTMono.txt` — PT Mono). Шрифты не продаются отдельно и не изменяются по начертаниям и глифам; зарезервированные имена «PT Sans», «PT Serif», «PT Mono», «ParaType» сохранены.

| Файл | Начертание | Источник (google/fonts, коммит `6e8069ff8ba3dab2a397fb30e7fbd243aba9b57a`) |
|---|---|---|
| `PTSans-Regular.woff2` | PT Sans 400 | https://github.com/google/fonts/blob/main/ofl/ptsans/PT_Sans-Web-Regular.ttf |
| `PTSans-Bold.woff2` | PT Sans 700 | https://github.com/google/fonts/blob/main/ofl/ptsans/PT_Sans-Web-Bold.ttf |
| `PTSansNarrow-Bold.woff2` | PT Sans Narrow 700 | https://github.com/google/fonts/blob/main/ofl/ptsansnarrow/PT_Sans-Narrow-Web-Bold.ttf |
| `PTMono-Regular.woff2` | PT Mono 400 | https://github.com/google/fonts/blob/main/ofl/ptmono/PTM55FT.ttf |

Файлы WOFF2 получены из исходных TTF только перекодированием (`woff2_compress`, пакет `wawoff2`) без подмножеств глифов; контуры совпадают с исходными (проверено `fontkit`).

Проверка файлов (версия «2.003W OFL» у PT Sans и PT Sans Narrow, «1.001W OFL» у PT Mono): глифы `№ ≥ — « » ₽ ё Ё` есть во всех четырёх файлах; стрелка `→` есть только в PT Mono. Табличные цифры (`tnum`) в PT Sans и PT Sans Narrow отсутствуют: у PT Sans доступны `aalt ccmp frac hist liga ordn sups case cpsp kern`, у PT Mono — `aalt c2sc dlig frac hist numr ordn sinf subs sups case`.
