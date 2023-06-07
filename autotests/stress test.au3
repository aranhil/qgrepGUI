Local $iWaitPerTest = 60000

Func CheckWindows()
    If Not WinActive("ctags_vs2013") And Not WinActive("qgrep GUI") Then Exit
EndFunc

HotKeySet("{F3}", "_Exit")

Func _Exit()
   Exit
EndFunc

Local $aWords = ["auto", "double", "int", "struct", "break", "else", "long", "switch", "case", "enum", "register", "typedef", "char", "extern", "return", "union", "const", "float", "short", "unsigned", "continue", "for", "signed", "void", "default", "goto", "sizeof", "volatile", "do", "if", "static", "while", "break", "const", "float", "short", "union", "while", "double", "struct", "switch", "register", "typedef", "extern", "return", "continue", "default", "sizeof", "volatile", "static", "public", "private", "friend", "inline", "delete", "class", "throw", "catch", "static_cast", "dynamic_cast", "reinterpret_cast", "const_cast", "namespace", "template", "typename", "virtual", "override", "nullptr", "boolean", "integer", "string", "vector", "deque", "list", "queue", "stack", "map", "set", "bitset", "tuple", "array", "function", "lambda", "thread", "mutex", "lock_guard", "unique_lock", "shared_lock", "condition_variable", "future", "promise", "packaged_task", "async", "chrono", "duration", "time_point", "system_clock", "steady_clock", "high_resolution_clock"]

Func random_fast_word()

	Local $sWord = $aWords[Random(0, UBound($aWords) - 1, 1)]
	Opt("SendKeyDelay", Random(10, 70, 1))
	Send($sWord)
	Send("^a")
   
EndFunc

Func random_word()

	Local $sWord = $aWords[Random(0, UBound($aWords) - 1, 1)]
	Opt("SendKeyDelay", Random(10, 250, 1))
	Send($sWord)
	Sleep(Random(0, 150, 1))
	Send("^a")
	Sleep(Random(0, 150, 1))
   
EndFunc

Func random_word_with_delete()

	Local $sWord = $aWords[Random(0, UBound($aWords) - 1, 1)]
	Opt("SendKeyDelay", Random(10, 250, 1))
	Send($sWord)
	Sleep(Random(0, 150, 1))
	Send("^a")
	Sleep(Random(0, 150, 1))
	Send("{DEL}")
	Sleep(Random(0, 150, 1))
   
EndFunc

Func random_word_with_random_down_navigation()

	Local $sWord = $aWords[Random(0, UBound($aWords) - 1, 1)]
	Opt("SendKeyDelay", Random(10, 250, 1))
	Send($sWord)

	Opt("SendKeyDelay", Random(20, 70, 1))
	For $i = 1 To Random(1, 20, 1)
		Send("{DOWN}")
	Next
	
	Sleep(20)
	Send("{TAB}")
	Send("^a")
	Sleep(Random(0, 150, 1))
   
EndFunc

Func random_word_with_random_navigation()

	Local $sWord = $aWords[Random(0, UBound($aWords) - 1, 1)]
	Opt("SendKeyDelay", Random(10, 250, 1))
	Send($sWord)

	Opt("SendKeyDelay", Random(10, 100, 1))
	For $i = 1 To Random(1, 10, 1)
		Send("{DOWN}")
		If Random(0, 1) > 0.5 Then
			Send("{PGDN}")
		EndIf
		If Random(0, 1) > 0.5 Then
			Send("{LEFT}{LEFT}")
		EndIf
	Next

	Sleep(20)
	Send("{TAB}")
	Send("^a")
	Sleep(Random(0, 150, 1))
   
EndFunc

Func random_word_with_random_everything()
	Local $switched = false

	Local $sWord = $aWords[Random(0, UBound($aWords) - 1, 1)]
	Opt("SendKeyDelay", Random(10, 250, 1))
	Send($sWord)

	Opt("SendKeyDelay", Random(10, 100, 1))
	For $i = 1 To Random(1, 10, 1)
		Send("{DOWN}")
		If Random(0, 1) > 0.5 Then
			Send("{PGDN}")
		EndIf
		If Random(0, 1) > 0.5 Then
			If $switched Then
				Send("{RIGHT}{RIGHT}")
			Else
				Send("{LEFT}{LEFT}")
			Endif
		EndIf
		If Random(0, 1) > 0.95 Then
			Send("!x")
			Sleep(100)
			$switched = Not $switched
		EndIf
	Next

	Sleep(20)
	Send("{TAB}")
	Send("^a")
	Sleep(Random(0, 150, 1))
   
EndFunc

Local $hTimer = TimerInit()
Sleep(3000)

While 1
	While 1
		random_fast_word()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_fast_word()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word_with_delete()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word_with_delete()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word_with_random_down_navigation()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word_with_random_down_navigation()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word_with_random_navigation()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word_with_random_navigation()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word_with_random_everything()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()

	While 1
		random_word_with_random_everything()
		CheckWindows()
		Local $iElapsedTime = TimerDiff($hTimer)
		If $iElapsedTime > $iWaitPerTest Then ExitLoop
	WEnd

	Sleep(1000)
	Send("!g")
	$hTimer = TimerInit()
WEnd