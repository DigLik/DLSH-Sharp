use libc::{WEXITSTATUS, WIFEXITED, waitpid, fork, execvp};
use shlex::split;
use std::ffi::CString;
use std::ptr;
use std::env;
use std::path::Path;
use std::io::{self, Write};


pub fn handle_builtin(line: &str) {
    let args: Vec<String> = match split(line) {
        Some(a) => a,
        None => {
            eprintln!("Command parsing error");
            return;
        }
    };
    if args.is_empty() {
        return;
    }

    match args[0].as_str() {
        "print" => {
            if args.len() > 1 {
                println!("{}", args[1]);
            } else {
                println!(); 
            }
        }
        "print-var" => {
            if args.len() < 2 {
                eprintln!("print-var: variable name required");
                return;
            }
            match env::var(&args[1]) {
                Ok(val) => println!("{}", val),
                Err(e) => eprintln!("varriable {}: {}", args[1], e),
            }
        }
        "cd" => {
            let target = if args.len() >= 2 {
                args[1].clone()
            } else {
                // без аргументов — переходим в HOME
                env::var("HOME").unwrap_or_else(|_| "/".to_string())
            };
            if let Err(e) = env::set_current_dir(Path::new(&target)) {
                eprintln!("cd: {}: {}", target, e);
            }
        }
        "var" => {
            if args.len() < 2 {
                eprintln!("var: key=value required");
                return;
            }
            if let Some((key, value)) = args[1].split_once('=') {
                unsafe {env::set_var(key, value);}
            } else {
                eprintln!("var: argument must be in key=value format");
            }
        }
        "exit" => {
            let code = args.get(1).and_then(|s| s.parse().ok()).unwrap_or(0);
            std::process::exit(code);
        }
        "clr" => {
            print!("\x1B[2J\x1B[H");
            io::stdout().flush().unwrap();
        }
        _ => {
            
            system_run(args);
        }
    }
}

fn system_run(args: Vec<String>) {
    unsafe {
        let pid = fork();
        match pid {
            -1 => panic!("Fork Failed!"),
            0 => {
                let c_args: Vec<CString> = args.iter()
                    .map(|arg| CString::new(arg.as_str()).expect("CString::new failed"))
                    .collect();

                let mut argv_ptrs: Vec<*const libc::c_char> = c_args.iter()
                    .map(|arg| arg.as_ptr())
                    .collect();

                argv_ptrs.push(ptr::null());
                execvp(argv_ptrs[0], argv_ptrs.as_ptr());
                panic!("execvp failed!");
            }
            child_pid => {
                let mut status: i32 = 0;
                let ret = waitpid(child_pid, &mut status, 0);
                if ret == -1 {
                    eprintln!("waitpid error");
                } else if WIFEXITED(status) {
                    let code = WEXITSTATUS(status);
                    if code != 0 {
                        eprintln!("the program terminated with code {}", code);
                    }
                }
            }
        }
    }
}