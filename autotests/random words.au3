#include <Array.au3>

; Set F3 as a hotkey to exit the script
HotKeySet("{F3}", "_Exit")

; Array of common C++ keywords
;Local $aWords = ["auto", "double", "int", "struct", "break", "else", "long", "switch", "case", "enum", "register", "typedef", "char", "extern", "return", "union", "const", "float", "short", "unsigned", "continue", "for", "signed", "void", "default", "goto", "sizeof", "volatile", "do", "if", "static", "while"]
Local $aWords = ["break", "const", "float", "short", "union", "while", "double", "struct", "switch", "register", "typedef", "extern", "return", "continue", "default", "sizeof", "volatile", "static", "public", "private", "friend", "inline", "delete", "class", "throw", "catch", "static_cast", "dynamic_cast", "reinterpret_cast", "const_cast", "namespace", "template", "typename", "virtual", "override", "nullptr", "boolean", "integer", "string", "vector", "deque", "list", "queue", "stack", "map", "set", "bitset", "tuple", "array", "function", "lambda", "thread", "mutex", "lock_guard", "unique_lock", "shared_lock", "condition_variable", "future", "promise", "packaged_task", "async", "chrono", "duration", "time_point", "system_clock", "steady_clock", "high_resolution_clock"]

; Wait for 5 seconds before starting
Sleep(2000)

; Infinite loop
While 1
   ; Select a random keyword
   Local $sWord = $aWords[Random(0, UBound($aWords) - 1, 1)]

   ; Set a random typing speed
   Opt("SendKeyDelay", Random(10, 100, 1))
   ;Opt("SendKeyDelay", 70)

   ; Type the word
   Send($sWord)
   
      ; 50/50 chance to send Down key presses
   If Random(0, 1) > 0.5 Then
      ; Send between 1-10 Down key presses
      For $i = 1 To Random(1, 10, 1)
         Send("{DOWN}")
      Next
   EndIf

   ; Send Shift+Tab
   Send("+{TAB}")

   ; Select all text
   Send("^a")
   
   ; Delete selected text
   Send("{DEL}")

   ; Wait for a random amount of time between 1 and 5 seconds
   Sleep(Random(10, 100, 1))
   ;Sleep(500)
   
   ; If the "qgrep GUI" window becomes inactive, exit the script
   If Not WinActive("ctags_vs2013") And Not WinActive("qgrep GUI") Then Exit
WEnd

; Function to exit the script
Func _Exit()
   Exit
EndFunc
