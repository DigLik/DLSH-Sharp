// Copyright (C) <2026> <Bogdan Yachmenv>
// SPDX-License-Identifier: GPL-3.0/

mod cmd_runner;
use rustyline::DefaultEditor;
use rustyline::error::ReadlineError;
use cmd_runner::handle_builtin;

fn main() {
    let mut rl = DefaultEditor::new().expect("Не удалось создать редактор");
    let _ = rl.load_history("history.txt");

    loop {
        match rl.readline(">> ") {
            Ok(line) => {
                let _ = rl.add_history_entry(line.as_str());
                handle_builtin(&line);
            }
            Err(ReadlineError::Interrupted | ReadlineError::Eof) => break,
            Err(err) => {
                eprintln!("Ошибка: {}", err);
                break;
            }
        }
    }

    rl.save_history("history.txt").expect("Не удалось сохранить историю");
}