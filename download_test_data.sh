#!/bin/bash

echo "🌐 Скачивание тестовых данных с askstudio.ru"
echo "=================================================="

# Создаем директории для тестовых данных
TEST_DATA_DIR="tests/Aggregator.Tests/TestData/HtmlPages/askstudio"
mkdir -p "$TEST_DATA_DIR"

# User Agent для имитации браузера
USER_AGENT="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36"

# Счетчик скачанных файлов
downloaded=0
total=3

# Функция для скачивания страницы
download_page() {
    local url="$1"
    local filename="$2"
    local filepath="$TEST_DATA_DIR/$filename"
    
    echo "Скачиваем: $url"
    
    if curl -s -A "$USER_AGENT" -o "$filepath" "$url"; then
        local size=$(wc -c < "$filepath")
        echo "✅ Сохранено: $filename ($size байт)"
        ((downloaded++))
    else
        echo "❌ Ошибка при скачивании: $url"
    fi
    
    # Задержка между запросами (2 секунды)
    sleep 2
}

# Скачиваем основные страницы для тестирования
echo
echo "Скачиваем страницы магазина..."

download_page "https://askstudio.ru/shop/" "main_shop_page.html"
download_page "https://askstudio.ru/shop/?page=2" "shop_page_2.html" 
download_page "https://askstudio.ru/shop/category/aksessuary/" "category_aksessuary.html"

echo
echo "=================================================="
echo "✅ Скачано $downloaded из $total файлов"
echo "📁 Файлы сохранены в: $TEST_DATA_DIR"
echo "🧪 Теперь можно использовать их в unit тестах!"

# Показываем список скачанных файлов
if [ $downloaded -gt 0 ]; then
    echo
    echo "📄 Скачанные файлы:"
    ls -la "$TEST_DATA_DIR"/*.html 2>/dev/null | awk '{print "   " $9 " (" $5 " байт)"}'
fi 