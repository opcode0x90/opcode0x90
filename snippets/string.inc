comment ^
 
 ;=========================;
 ;     String Macro(s)     ;
 ;=========================;
 
 Written by Opcode0x90 (Opcode0x90@hotmail.com)
 Your ideas and suggestions are welcomed.
 
 Changelog:
    
    21 December 2006 - First version
                     + Supports both ASCII and Unicode version of string
                     + Supports string pooling
 
 ================================================================================================
 
 This is a collection of string macro(s) to ease the programmer when defining
 string(s) in their assembly.
 
 Currently this macro supports both ASCII and Unicode version of string, and
 also supports escaped characters.
 
 List of public macro(s).
    
    String(Text)
    StringA(Text)
    StringW(Text)
    _String(Text)
    _StringA(Text)
    _StringW(Text)
    _UNICODE_STRING(Text)
    __UNICODE_STRING(Text)
    
 The following macro(s) is reserved for internal usage and is not encouraged
 for public use.
    
    StringMacInit()
	StringPoolLocate(Identifier)
	StringPoolRegister(Identifier)
    GenerateCharList(Text)
    GenerateUniqueIdentifier(Text)
    CalculateStringLength(Text)
    CalculateIdentifierSize(Text)
    
 ================================================================================================
 
 The behaviour of String() macro depends on global variable UNICODE. If you wish
 to explicitly define ASCII or Unicode string, you can use StringA() or StringW()
 instead.
 
 To define unicode strings by default, just define

    UNICODE = 1
        or 
    UNICODE equ 1
 
 in front of your assembly.
 
 ================================================================================================
 
 - Escape Character
 
 All the public macro(s) supports escape character. You MUST begin with a backslash ("\")
 followed by the escape character.
    
    --------------------------------------------------
    esc. char.     code         symbol
    --------------------------------------------------
        \\         5Ch            "\"
        \e         21h            "!"				;; To workaround collision with MASM macro syntax
        \s         27h            "'"				;; To workaround collision with MASM macro syntax
        \n         0Ah            newline
        \t         09h            horizontal tab
        \v         0Bh            vertical tab
        \r         0Dh            carriage return
        \f         0Ah            formfeed
        \a         07h            alert
        \q         22h            double-quote
        \0         00h            null character
        \b         08h            backspace
 
 ================================================================================================
 
 - Alignment
 
 Every string defined by macro(s) is aligned to 4 bytes by default. This behaviour depends on
 global variable STRING_ALIGNMENT, which is 4 initially. You can override default alignment
 by defining STRING_ALIGNMENT in front of your assembly.
 
 Alignment can be immediate value of 1 (byte), 2 (word) or 4 (dword).
 
 ================================================================================================
 
 - String Pooling
 
 This feature attempts to prevent duplicated strings in your assembly. So only a single copy
 of identical strings will present in the program image, resulting in smaller programs.
 
 However, this limits the number of characters allowed in a string. To workaround that
 limitation, STRING_POOLING is set to 1 by default. When encountering a string longer than 
 240 characters, string pooling will be temporary disabled.
 
 These are three settings acceptable for STRING_POOLING:
 
 STRING_POOLING = 0   ; disable string pooling completely
 STRING_POOLING = 1   ; auto string pooling (default)
 STRING_POOLING = 2   ; forced string pooling
 
 You are adviced set STRING_POOLING = 1. The other settings may result in duplicated strings
 within program image or causes assembler error. See Known Issues for further details.
 
 ================================================================================================
 
 - Known Issues
 
 When you define a string more than 240 characters, you may get these following assembler error -
    error A2039: line too long
    error A2041: string or text literal too long
    error A2042: statement too complex
    error A2043: identifier too long
    
 You should set STRING_POOLING = 1. If the problem persists, you can disable string pooling
 to workaround this problem.
 
 ================================================================================================
 
 - Examples:
 
 invoke MessageBox, NULL, String("some message"), String("String Macro Example"), MB_OK
 
      or in unicode string
      
 invoke MessageBoxW, NULL, StringW("some message"), StringW("String Macro Example"), MB_OK
 
^

String macro Text
	exitm <offset _String(Text)>
endm

StringA macro Text
	exitm <offset _StringA(Text)>
endm

StringW macro Text
	exitm <offset _StringW(Text)>
endm

_UNICODE_STRING macro Text
	exitm <offset __UNICODE_STRING(Text)>
endm

_String macro Text
%	
	ifndef UNICODE
	%	; ASCII
		exitm StringA(Text)
	elseif UNICODE eq 0
	%	; still ASCII
		exitm StringA(Text)
	else
	%	; Unicode
		exitm StringW(Text)
	endif
%	
endm

_StringA macro Text
	LOCAL Identifier
	LOCAL SegName
%	
	if STRING_POOLING gt 0
	%	; is this string contains more than 240 characters ?
		if (STRING_POOLING eq 1) and (CalculateIdentifierSize(Text) ge 240)
		%	; no string pooling
		else
		%	; generate unique identifier for this string
			Identifier textequ <@CatStr(%GenerateUniqueIdentifier(Text), <A>)>
		%	
		%	; is this string defined ?
			if StringPoolLocate(Identifier) eq 1
			%	; use this string instead
				exitm <Identifier>
			endif
		%	
		%	; add this string into pool database
			StringPoolRegister %Identifier
		endif
	endif
%	
%	; preserve the segment name
	SegName textequ @CurSeg
%	
%	; define the string
	.const
		if STRING_ALIGNMENT gt 1
		%	; align the string to specified value
		%	align STRING_ALIGNMENT
		else
		%	; default alignment
			align 2
		endif
	%	
		Identifier db 0 dup (?)
		GenerateCharList <db>, Text
		db 0
%	
%	; restore the original segment
	@CurSeg ends
	SegName segment
%	
	exitm <Identifier>
endm

_StringW macro Text
	LOCAL Identifier
	LOCAL SegName
%	
	if STRING_POOLING gt 0
	%	; is this string contains more than 240 characters ?
		if (STRING_POOLING eq 1) and (CalculateIdentifierSize(Text) ge 240)
		%	; no string pooling
		else
		%	; generate unique identifier for this string
			Identifier textequ <@CatStr(%GenerateUniqueIdentifier(Text), <W>)>
		%	
		%	; is this string defined ?
			if StringPoolLocate(Identifier) eq 1
			%	; use this string instead
				exitm <Identifier>
			endif
		%	
		%	; add this string into pool database
			StringPoolRegister %Identifier
		endif
	endif
%	
%	; preserve the segment name
	SegName textequ @CurSeg
%	
%	; define the string
	.const
		if STRING_ALIGNMENT gt 2
		%	; align the string to specified value
		%	align STRING_ALIGNMENT
		else
		%	; default alignment
			align 2
		endif
	%	
		Identifier dw 0 dup (?)
		GenerateCharList <dw>, Text
		dw 0
%	
%	; restore the original segment
	@CurSeg ends
	SegName segment
%	
	exitm <Identifier>
endm

__UNICODE_STRING macro Text
	LOCAL StrId
	LOCAL StrLen
	LOCAL Identifier
	LOCAL SegName
	LOCAL Line
%	
	ifdef UNICODE_STRING
		if STRING_POOLING gt 0
		%	; is this string contains more than 240 characters ?
			if (STRING_POOLING eq 1) and (CalculateIdentifierSize(Text) ge 240)
			%	; no string pooling
			else
			%	; generate unique identifier for this string
				Identifier textequ <@CatStr(%GenerateUniqueIdentifier(Text), <US>)>
			%	
			%	; is this string defined ?
				if StringPoolLocate(Identifier) eq 1
				%	; use this string instead
					exitm <Identifier>
				endif
			%	
			%	; add this string into pool database
				StringPoolRegister %Identifier
			endif
		endif
	%	
	%	; define the unicode string and calculate its size
		StrId textequ <StringW(Text)>
		StrLen = CalculateStringLength(Text)
	%	
	%	; preserve the segment name
		SegName textequ @CurSeg
	%	
	%	; define the structure
		.const
			Identifier UNICODE_STRING {(StrLen * 2), (StrLen * 2) + 2, StrId}
	%	
	%	; restore the original segment
		@CurSeg ends
		SegName segment
	%	
		exitm <Identifier>
	else
	%	; UNICODE_STRING structure is not defined
		Line textequ %@Line
		%echo @FileCur(Line) : _UNICODE_STRING error : UNICODE_STRING is not defined
		exitm <0>
	endif
%	
endm

StringMacInit macro
%	
	ifndef _STRING_INIT
	%	; string macro is initialized
		_STRING_INIT = 1
	%	
	%	; uncomment this line to define unicode string instead of ascii string
		; UNICODE = 1
	%	
		ifndef STRING_ALIGNMENT
		%	; number of bytes to align (0 = no alignment)
			STRING_ALIGNMENT = 4
		endif
	%	
		ifndef STRING_POOLING
		%	; 0 = deactivate string pooling
		%	; 1 = auto string pooling (see Known Issues for further details)
		%	; 2 = forced string pooling
			STRING_POOLING = 1
		endif
	%	
		if STRING_POOLING gt 0
		%	; initialize string pooling variables
			STRING_POOLING_COUNT    = 0
		%	
			STRING_POOLING_PREFIX   textequ <@CatStr(<sz>, @SubStr(%@Time, 1, 2), @SubStr(%@Time, 4, 2), @SubStr(%@Time, 7, 2))>
			STRING_POOLING_DATABASE textequ <@CatStr(<PDB>, @SubStr(%@Time, 7, 2), @SubStr(%@Time, 1, 2), @SubStr(%@Time, 4, 2))>
		endif
	endif
%	
endm

CalculateIdentifierSize macro Text
	LOCAL Pos
	LOCAL IdSize
%	
%	; initialize the macro variables
	Pos    = 0
	IdSize = 0
%	
%	; generate unique hash
	forc Char, <Text>
	%	; increment position
		Pos = Pos + 1
	%	
	%	; substitute symbols with random string
		if (Pos ne 1) and (Pos ne @SizeStr(Text))
			if "&Char" eq " "
				IdSize = IdSize + 6
			elseif "&Char" eq "\"
				IdSize = IdSize + 6
			elseif "&Char" eq "'"
				IdSize = IdSize + 6
			elseif "&Char" eq "|"
				IdSize = IdSize + 6
			elseif "&Char" eq "["
				IdSize = IdSize + 6
			elseif "&Char" eq "]"
				IdSize = IdSize + 6
			elseif "&Char" eq "{"
				IdSize = IdSize + 6
			elseif "&Char" eq "}"
				IdSize = IdSize + 6
			elseif "&Char" eq "?"
				IdSize = IdSize + 6
			elseif "&Char" eq ","
				IdSize = IdSize + 6
			elseif "&Char" eq "."
				IdSize = IdSize + 6
			elseif "&Char" eq "/"
				IdSize = IdSize + 6
			elseif "&Char" eq ";"
				IdSize = IdSize + 6
			elseif "&Char" eq ":"
				IdSize = IdSize + 6
			elseif "&Char" eq "'"
				IdSize = IdSize + 6
			elseif "&Char" eq "`"
				IdSize = IdSize + 6
			elseif "&Char" eq "~"
				IdSize = IdSize + 6
			elseif "&Char" eq "@"
				IdSize = IdSize + 6
			elseif "&Char" eq "#"
				IdSize = IdSize + 6
			elseif "&Char" eq "$"
				IdSize = IdSize + 6
			elseif "&Char" eq "%"
				IdSize = IdSize + 6
			elseif "&Char" eq "^"
				IdSize = IdSize + 6
			elseif "&Char" eq "&"
				IdSize = IdSize + 6
			elseif "&Char" eq "*"
				IdSize = IdSize + 6
			elseif "&Char" eq "("
				IdSize = IdSize + 6
			elseif "&Char" eq ")"
				IdSize = IdSize + 6
			elseif "&Char" eq "_"
				IdSize = IdSize + 6
			elseif "&Char" eq "+"
				IdSize = IdSize + 6
			elseif "&Char" eq "-"
				IdSize = IdSize + 6
			elseif "&Char" eq "="
				IdSize = IdSize + 6
			else
				IdSize = IdSize + 1
			endif
		endif
	endm
%	
%	; return the hash
	exitm <IdSize>
endm

GenerateUniqueIdentifier macro Text
	LOCAL Pos
	LOCAL Hash
%	
%	; initialize the macro variables
	Pos  = 0
	Hash textequ <>
%	
%	; generate unique hash
	forc Char, <Text>
	%	; increment position
		Pos = Pos + 1
	%	
	%	; substitute symbols with random string
		if (Pos ne 1) and (Pos ne @SizeStr(Text))
			if "&Char" eq " "
				Hash catstr Hash, <v1m9m3>
			elseif "&Char" eq "\"
				Hash catstr Hash, <k2f1k4>
			elseif "&Char" eq "|"
				Hash catstr Hash, <f23fsd>
			elseif "&Char" eq "["
				Hash catstr Hash, <f8g5d9>
			elseif "&Char" eq "]"
				Hash catstr Hash, <h3f6g5>
			elseif "&Char" eq "{"
				Hash catstr Hash, <sdh9s8>
			elseif "&Char" eq "}"
				Hash catstr Hash, <yw54y4>
			elseif "&Char" eq "?"
				Hash catstr Hash, <w3trt3>
			elseif "&Char" eq ","
				Hash catstr Hash, <q3tdt3>
			elseif "&Char" eq "."
				Hash catstr Hash, <h45hgw>
			elseif "&Char" eq "/"
				Hash catstr Hash, <d2g3wg>
			elseif "&Char" eq ";"
				Hash catstr Hash, <f32ffa>
			elseif "&Char" eq ":"
				Hash catstr Hash, <rc5r2c>
			elseif "&Char" eq "'"
				Hash catstr Hash, <nf32uf>
			elseif "&Char" eq "`"
				Hash catstr Hash, <jnsdg3>
			elseif "&Char" eq "~"
				Hash catstr Hash, <qvdfg3>
			elseif "&Char" eq "@"
				Hash catstr Hash, <skg093>
			elseif "&Char" eq "#"
				Hash catstr Hash, <qm93m9>
			elseif "&Char" eq "$"
				Hash catstr Hash, <mvsm29>
			elseif "&Char" eq "%"
				Hash catstr Hash, <tk34mt>
			elseif "&Char" eq "^"
				Hash catstr Hash, <dsf89a>
			elseif "&Char" eq "&"
				Hash catstr Hash, <gm4932>
			elseif "&Char" eq "*"
				Hash catstr Hash, <n2783c>
			elseif "&Char" eq "("
				Hash catstr Hash, <t8gj39>
			elseif "&Char" eq ")"
				Hash catstr Hash, <d8g79s>
			elseif "&Char" eq "_"
				Hash catstr Hash, <hm283v>
			elseif "&Char" eq "+"
				Hash catstr Hash, <mx8r29>
			elseif "&Char" eq "-"
				Hash catstr Hash, <ds089n>
			elseif "&Char" eq "="
				Hash catstr Hash, <j32847>
			else
				Hash catstr Hash, <Char>
			endif
		endif
	endm
%	
%	; return the hash
	exitm <@CatStr(%STRING_POOLING_PREFIX, %Hash, %@SizeStr(Text))>
endm

GenerateCharList macro Prefix, Text
	LOCAL Pos
	LOCAL IsEsc
%	
%	; initialize the macro variables
	Pos   = 0
	IsEsc = 0
%	
%	; generate unique hash
	forc Char, <Text>
	%	; increment position
		Pos = Pos + 1
	%	
	%	; generate string list
		if (Pos ne 1) and (Pos ne @SizeStr(Text))
			if IsEsc eq 1
				if "&Char" eq "\"
				%	; backslash
					Prefix "\"
				elseif "&Char" eq "n"
				%	; newline
					Prefix 0Ah
				elseif "&Char" eq "t"
				%	; horizontal tab
					Prefix 09h
				elseif "&Char" eq "v"
				%	; vertical tab
					Prefix 0Bh
				elseif "&Char" eq "r"
				%	; carriage return
					Prefix 0Dh
				elseif "&Char" eq "f"
				%	; formfeed
					Prefix 0Bh
				elseif "&Char" eq "a"
				%	; alert
					Prefix 07h
				elseif "&Char" eq "s"
				%	; single-quote
					Prefix 027h
				elseif "&Char" eq "q"
				%	; double-quote
					Prefix """"
				elseif "&Char" eq "b"
				%	; backspace
					Prefix 08h
				elseif "&Char" eq "0"
				%	; null character
					Prefix 00h
				elseif "&Char" eq "e"
				%	; exclamation character
					Prefix 21h
				else
				%	; unreconised character constant
					.err <unreconised character escape sequence: \&Char>
					exitm <>
				endif
				
			%	; escape character handled
				IsEsc = 0
			else
				if "&Char" eq "\"
				%	; escape character
					IsEsc = 1
				else
					Prefix "&Char"
				endif
			endif
		endif
	endm
%	
endm

CalculateStringLength macro Text
	LOCAL Pos
	LOCAL IsEsc
	LOCAL StrLen
%	
%	; initialize the macro variables
	Pos    = 0
	IsEsc  = 0
	StrLen = 0
%	
%	; generate unique hash
	forc Char, <Text>
	%	; increment position
		Pos = Pos + 1
	%	
	%	; generate string list
		if (Pos ne 1) and (Pos ne @SizeStr(Text))
			if IsEsc eq 1
				if "&Char" eq "\"
				%	; backslash
					StrLen = StrLen + 1
				elseif "&Char" eq "n"
				%	; newline
					StrLen = StrLen + 1
				elseif "&Char" eq "t"
				%	; horizontal tab
					StrLen = StrLen + 1
				elseif "&Char" eq "v"
				%	; vertical tab
					StrLen = StrLen + 1
				elseif "&Char" eq "r"
				%	; carriage return
					StrLen = StrLen + 1
				elseif "&Char" eq "f"
				%	; formfeed
					StrLen = StrLen + 1
				elseif "&Char" eq "a"
				%	; alert
					StrLen = StrLen + 1
				elseif "&Char" eq "q"
				%	; double-quote
					StrLen = StrLen + 1
				elseif "&Char" eq "b"
				%	; backspace
					StrLen = StrLen + 1
				elseif "&Char" eq "0"
				%	; null character
					StrLen = StrLen + 1
				elseif "&Char" eq "e"
				%	; exclamation character
					StrLen = StrLen + 1
				else
				%	; unreconised character constant
					.err <unreconised character escape sequence: \&Char>
					exitm <>
				endif
				
			%	; escape character handled
				IsEsc = 0
			else
				if "&Char" eq "\"
				%	; escape character
					IsEsc = 1
				else
					StrLen = StrLen + 1
				endif
			endif
		endif
	endm
%	
%	; return
	exitm <StrLen>
endm

StringPoolRegister macro Identifier
%	; register this string
	@CatStr(%STRING_POOLING_DATABASE, %STRING_POOLING_COUNT) textequ <Identifier>
%	
%	; increment string count
	STRING_POOLING_COUNT = STRING_POOLING_COUNT + 1
%	
endm

StringPoolLocate macro Identifier
	LOCAL Count
	LOCAL Result
%	
%	; initialize the macro variables
	Count  = 0
	Result = 0
%	
%	; locate string within pool database
	while Count le STRING_POOLING_COUNT
	%	; is this the string we are looking for ?
		if @InStr(1, %@CatStr(%STRING_POOLING_DATABASE, %Count), %Identifier) eq 1
		%	; yes, it is
			Result = 1
		%	
		%	; break
			Count = STRING_POOLING_COUNT
		%	
		endif
	%	
	%	; move to next string
		Count = Count + 1
	endm
%	
%	; return
	exitm <Result>
endm

; initialize the string macro (if not initialized)
StringMacInit
