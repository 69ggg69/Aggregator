# Mermaid DB structure:
(try at [mermaid.live](https://mermaid.live/) or [mermaidflow.app](https://www.mermaidflow.app/editor))

```mermaid
---
config:
      theme: neutral
---
erDiagram
    SHOPS {
        INTEGER id PK "Уникальный идентификатор магазина"
        VARCHAR name "Название магазина"
        VARCHAR url "URL магазина"
        TEXT description "Описание магазина"
        TIMESTAMP created_at "Дата добавления"
        TIMESTAMP updated_at "Дата последнего обновления"
    }

    PRODUCTS {
        INTEGER id PK "Уникальный идентификатор товара"
        VARCHAR name "Название товара"
        TEXT description "Описание товара (NULL до детального парсинга)"
        ENUM audience "Мужское/Женское/Детское/Унисекс (NULL до детального парсинга)"
        INTEGER shop_id FK "Магазин (FK → shops.id)"
        VARCHAR material FK "Материал (FK → materials.id, NULL до детального парсинга)"
        VARCHAR product_url "Ссылка на страницу товара для детального парсинга"
        ENUM parsing_status "Статус парсинга: NotParsed, BasicParsed, DetailedParsed"
        TIMESTAMP created_at "Дата добавления"
        TIMESTAMP updated_at "Дата последнего обновления"
    }
    
    CATEGORIES {
        INTEGER id PK "Уникальный идентификатор категории"
        VARCHAR name "Название категории"
        INTEGER parent_id FK "Родительская категория (NULL для корня)"
    }

    TAGS {
        INTEGER id PK "Уникальный идентификатор тега"
        VARCHAR name "Название тега"
    }

    PRODUCT_CATEGORIES {
        INTEGER product_id FK "Ссылка на товар из таблицы products"
        INTEGER category_id FK "Ссылка на категорию из таблицы categories"
    }

    PRODUCT_TAGS {
        INTEGER product_id FK "Ссылка на товар из таблицы products"
        INTEGER tag_id FK "Ссылка на тег из таблицы tags"
    }

    COLORS {
        INTEGER id PK "Уникальный идентификатор цвета"
        VARCHAR name "Название цвета"
        VARCHAR hex_code "HEX код цвета"
    }

    SIZES {
        INTEGER id PK "Уникальный идентификатор размера"
        VARCHAR name "Название размера (S, M, L, XL)"
    }

    IMAGES {
        INTEGER id PK
        INTEGER variant_id FK
        VARCHAR path
        VARCHAR alt_text
        INTEGER sort_order
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }

    MATERIALS {
        INTEGER id PK "Уникальный идентификатор материала"
        VARCHAR name "Название материала"
    }

    PRODUCT_VARIANTS {
        INTEGER id PK "Уникальный идентификатор варианта товара"
        INTEGER product_id FK "Ссылка на товар из таблицы products"
        INTEGER color_id FK "Ссылка на цвет из таблицы colors"
        INTEGER size_id FK "Ссылка на размер из таблицы sizes"
        INTEGER picture_id FK "Ссылка на картинку из таблицы IMAGES"
        VARCHAR sku "Уникальный идентификатор варианта товара"
        DECIMAL price "Цена товара"
        TIMESTAMP created_at "Дата добавления"
        TIMESTAMP updated_at "Дата последнего обновления"
    }

    AVAILABILITY {
        INTEGER id PK "Уникальный идентификатор записи"
        INTEGER variant_id FK "Ссылка на вариант товара из таблицы product_variants"
        INTEGER quantity "Количество товара в наличии"
        BOOLEAN is_available "Признак наличия товара (True/False)"
        TIMESTAMP updated_at "Дата последнего обновления"
    }

    PRODUCTS ||--o{ PRODUCT_CATEGORIES : assigns
    CATEGORIES ||--o{ PRODUCT_CATEGORIES : contains
    PRODUCTS ||--o{ PRODUCT_TAGS : tagged_with
    TAGS ||--o{ PRODUCT_TAGS : used_in
    PRODUCTS ||--o{ PRODUCT_VARIANTS : has
    COLORS ||--o{ PRODUCT_VARIANTS : defines
    SIZES ||--o{ PRODUCT_VARIANTS : defines
    PRODUCT_VARIANTS ||--o{ IMAGES : shows
    PRODUCT_VARIANTS ||--o{ AVAILABILITY : tracks
    MATERIALS ||--o{ PRODUCTS : defines
    SHOPS ||--o{ PRODUCTS : sells

```

## Пример записи

**Этап 1 - Базовый парсинг:**
В shops создаём магазин «MyShop» (url = "https://myshop.com").
В products создаём «Пуховик зимний» (name = "Пуховик зимний", shop_id = MyShop.id, product_url = "https://myshop.com/jackets/winter-jacket", parsing_status = BasicParsed).
Поля description, audience, material остаются NULL.

**Этап 2 - Детальный парсинг:**
Обновляем запись: description = "Тёплый зимний пуховик", audience = Unisex, material = «пух/перо», parsing_status = DetailedParsed.
Через product_categories связываем его с категориями «Верхняя одежда» → «Пуховики».
Через product_tags добавляем теги «Лёгкий», «Водонепроницаемый».
В product_variants создаём варианты:

    color = «Чёрный», size = «M», price = 199.99, sku = «JKT-BLK-M»
    color = «Синий», size = «L», price = 199.99, sku = «JKT-BLU-L»

В availability для каждого variant_id указываем quantity и is_available.

## Эта архитектура сочетает:

Строгую иерархию категорий для навигации по дереву.
Гибкие теги для любых дополнительных признаков.
Варианты и наличие для цветов/размеров.
