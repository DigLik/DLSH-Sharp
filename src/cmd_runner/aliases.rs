// Copyright (C) <2026> <Bogdan Yachmenv>
// SPDX-License-Identifier: GPL-3.0/

use std::collections::HashMap;
use std::sync::Mutex;
use lazy_static::lazy_static;

lazy_static! {
    static ref ALIASES: Mutex<HashMap<String, String>> = Mutex::new(HashMap::new());
}

/// Добавить или обновить алиас
pub fn add(name: &str, command: &str) {
    ALIASES.lock().unwrap().insert(name.to_string(), command.to_string());
}

/// Получить команду по имени алиаса (если существует)
pub fn get(name: &str) -> Option<String> {
    ALIASES.lock().unwrap().get(name).cloned()
}

/// Удалить алиас по имени
pub fn remove(name: &str) {
    ALIASES.lock().unwrap().remove(name);
}

/// Получить список всех алиасов (для команды `alias` без аргументов)
pub fn list() -> Vec<(String, String)> {
    ALIASES.lock().unwrap()
        .iter()
        .map(|(k, v)| (k.clone(), v.clone()))
        .collect()
}

/// Очистить все алиасы (на всякий случай)
pub fn clear() {
    ALIASES.lock().unwrap().clear();
}