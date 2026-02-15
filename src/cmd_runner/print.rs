// Builtin module for SBSH
use std::env;

pub fn print(args: Vec<String>) {
    if args.len() < 2 {
        eprintln!("print: missing arguments");
        return;
    }

    let mut output = Vec::new();

    for arg in args.iter().skip(1) {
        if arg == ";" {
            break;
        }

        if let Some(var_name) = arg.strip_prefix('$') {
            match env::var(var_name) {
                Ok(val) => output.push(val),
                Err(_) => {
                    eprintln!("Varriable not searched!");
                }
            }
        } else {
            output.push(arg.clone());
        }
    }
    println!("{}", output.join(" "));
}
