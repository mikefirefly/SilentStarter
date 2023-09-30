# SilentStarter 1.0

This utility starts a program in the background without showing any window. It can be used to start programs from Windows Task Scheduler.

Usage:
```SilentStarter.exe [/LD10] "C:\Users\John Doe\Documents\My Scripts\My Script.bat" [param1 param2 "param three"]```

Optional switches:

```/L```  : append the script execution log to the ```SilentStarter.log``` file in program directory

```/Dn``` : delay the script execution by ```n``` seconds

Supported script types:
- ```BAT``` / ```CMD```
- ```PS1``` (requires script execution policy set to ```Unrestricted```)
- ```PY``` (requires a valid Python installation)
