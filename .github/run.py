"""Runs the program in a terminal, as a reader would: python run.py path/to/lees-bybel"""
import re
import sys
import threading
import time

if sys.platform == "win32":
    from winpty import PtyProcess
else:
    from ptyprocess import PtyProcessUnicode as PtyProcess

CURSOR = "\x1b[6n"      # asks where the cursor is
seen = ""


def watch():
    global seen
    try:
        while True:
            text = p.read(4096)
            seen += text
            if CURSOR in text:
                p.write("\x1b[1;1R")
    except EOFError:
        pass


def send(keys, expected):
    """Types the keys and waits for the text on the screen."""
    global seen
    seen = ""
    for key in keys:
        p.write(key)
        time.sleep(0.1)
    end = time.time() + 20
    while time.time() < end:
        plain = re.sub(r"\x1b\[[?0-9;]*[A-Za-z]|\x1b\][^\x07]*\x07", "", seen)
        if expected in plain:
            print("ok:", repr(keys), "->", expected)
            return
        time.sleep(0.1)
    sys.exit(f"FAILED: {keys!r} did not show {expected!r}. Seen:\n{seen!r}")


p = PtyProcess.spawn([sys.argv[1], "psa"], dimensions=(24, 90))
threading.Thread(target=watch, daemon=True).start()
time.sleep(3)
send("/joh 3:16\r", "so lief het God")
send("/wie is jy dat jy bang is\r", "Jesaja 51:12")
send("\x1b", "? hulp")
send("/Bet-El El\r", "f: Bet-El El")
send("\x1b", "? hulp")
send("f", "f: Bet-El El")
send("\x1b", "? hulp")
send("?", "Versnommers")
send(" ", "? hulp")
p.write("q")
end = time.time() + 20
while p.isalive() and time.time() < end:
    time.sleep(0.1)
if p.isalive():
    sys.exit("FAILED: q did not close it")
if p.exitstatus:
    sys.exit(f"FAILED: exit status {p.exitstatus}. Seen:\n{seen!r}")
print("ok: q closes it")
