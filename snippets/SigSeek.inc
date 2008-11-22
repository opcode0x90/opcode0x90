;=======================================;
;     Code Signature Seek Functions     ;
;=======================================;

.code
	
	; function prototyping
	SigSeek_Compare		proto lpString1:DWORD, lpString2:DWORD, _Length:DWORD
	SigSeek_FindCode	proto dwStart:DWORD, dwEnd:DWORD, lpSig:DWORD, dwSize:DWORD
	SigSeek_SkipJack	proto C dwStart:DWORD, dwEnd:DWORD, dwOffset:SDWORD, dwArgs:DWORD, Args:VARARG
	
	SigSeek_FindCode proc uses ebx ecx edx esi edi dwStart:DWORD, dwEnd:DWORD, lpSig:DWORD, dwSize:DWORD
		
		;
		; EBX = dwSize
		; EDX = dwEnd - dwSize
		;
		
		; load the arguments into register
		mov ebx, dwSize
		mov edx, dwEnd
		sub edx, ebx
		
		; scan for specified signature
		mov ecx, ebx
		mov esi, dwStart
		mov edi, lpSig
		.repeat
			; compare the string
			repe cmpsb
			
			.if zero?
				; found the signature, return the address
				mov eax, ebx
				sub eax, ecx
				sub esi, eax
				mov eax, esi
				jmp @f
			.endif
			
			; restore ECX, ESI, EDI registers
			mov eax, ebx
			sub eax, ecx
			sub esi, eax
			sub edi, eax
			mov ecx, ebx
			
			; move to next byte
			add esi, 1
			
		.until (esi == edx)
		
		; return FALSE
		xor eax, eax
		
	@@:
		ret
		
	SigSeek_FindCode endp
	
	SigSeek_Compare proc uses ecx esi edi lpString1:DWORD, lpString2:DWORD, _Length:DWORD
		
		pushfd
			
			mov esi, lpString1
			mov edi, lpString2
			mov ecx, _Length
			cld
			repe cmpsb
			
			.if zero?
				; string match
				mov eax, TRUE
			.else
				; no match
				xor eax, eax
			.endif
			
		popfd
		
		ret
		
	SigSeek_Compare endp
	
	SigSeek_SkipJack proc C uses ebx ecx edx esi edi dwStart:DWORD, dwEnd:DWORD, dwOffset:SDWORD, dwArgs:DWORD, Args:VARARG
		
		.if (dwArgs > 0)
			mov ebx, dwStart
			.while TRUE
				; find the first code signature
				lea esi, Args
				invoke SigSeek_FindCode, ebx, dwEnd, [esi], [esi+4]
				
				.if (eax != NULL)
					mov ebx, eax
					mov edx, eax
					
					; skip the instruction length and specified number of bytes
					add ebx, [esi+4]
					add ebx, [esi+8]
					
					; initialize the registers
					mov ecx, 1
					mov edi, 1
					add esi, 12
					
					.repeat
						; is the next code signature match?
						invoke SigSeek_Compare, ebx, [esi], [esi+4]
						
						.if (eax != NULL)
							; skip the instruction length and specified number of bytes
							add ebx, [esi+4]
							add ebx, [esi+8]
							
							; increment match count
							inc edi
						.else
							; no match
							.break
						.endif
						
						; move to next argument
						inc ecx
						add esi, 12
						
					.until (ecx == dwArgs)
					
					.if (edi == dwArgs)
						; correct the address with specified offset
						add edx, dwOffset
						
						; return
						mov eax, edx
						.break
					.endif
				.else
					; no match
					xor eax, eax
					.break
				.endif
			.endw
		.else
			; invalid argument
			xor eax, eax
		.endif
		
		ret
		
	SigSeek_SkipJack endp
	