#include <iostream>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include "helper.h"

using std::cout;
using std::endl;
using std::hex;


HMODULE hKernel32 = GetModuleHandle("kernel32.dll");

/*
 * 7C810659 > 33ED        XOR EBP, EBP
 * 7C81065B   53          PUSH EBX
 * 7C81065C   50          PUSH EAX
 * 7C81065D   6A 00       PUSH 0
 * 7C81065F ^ E9 E8AFFFFF JMP 7C80B64C ; kernel32.BaseThreadStart
 */
LPSTR pSig                     = "\x33\xED\x53\x50\x6A\xE9";
DWORD BaseThreadStartThunk     = FindCode(pSig, sizeof(pSig), GetTextAddress(hKernel32), GetTextSize(hKernel32));
DWORD BaseThreadStartThunk_Jmp = (BaseThreadStartThunk + 0x6);
DWORD BaseThreadStart          = (BaseThreadStartThunk_Jmp + (int)*(DWORD*)(BaseThreadStartThunk_Jmp + 1) + 5);


bool IsSuspiciousAddress(DWORD dwAddress)
{
	static DWORD lpLoadLibraryA = (DWORD)GetProcAddress(hKernel32, "LoadLibraryA");
	static DWORD lpLoadLibraryW = (DWORD)GetProcAddress(hKernel32, "LoadLibraryW");
	static DWORD lpExitProcess  = (DWORD)GetProcAddress(hKernel32, "ExitProcess");
	static DWORD lpFreeLibrary  = (DWORD)GetProcAddress(hKernel32, "FreeLibrary");

	MEMORY_BASIC_INFORMATION MemInfo;
	
	// hey watch where you're going
	if ((dwAddress == lpLoadLibraryA) || (dwAddress == lpLoadLibraryW) || (dwAddress == lpExitProcess) || (dwAddress == lpFreeLibrary))
	{
		// obviously it shouldnt be pointing to these functions
		return true;
	}

	// check for memory protection type
	if (VirtualQuery((LPCVOID)dwAddress, &MemInfo, sizeof(MemInfo)))
	{
		// since injected code wont have MEM_IMAGE flag,
		// we'll check for that
		if (MemInfo.Type != MEM_IMAGE) return true;
	}

	return false;
}

void __hook_BaseThreadStart(LPTHREAD_START_ROUTINE lpStartAddress, LPVOID lpParameters, DWORD Unknown)
{
	// suspicious address ?
	if (IsSuspiciousAddress((DWORD)lpStartAddress))
	{
		// gotcha
		cout << "Suspicious thread blocked" << endl
			 << "  lpStartAddress: 0x" << hex << lpStartAddress<< endl
			 << "  lpParameters:   0x" << hex << lpParameters << endl
			 << endl;

		// kill this thread
		ExitThread(NULL);
	}
	else
	{
		// seems legitimate
		__asm
		{
			push lpParameters
			push lpStartAddress
			push Unknown
			jmp BaseThreadStart
		}
	}
}

void TestThread(DWORD lpParameters)
{
	// verify the parameter
	if (lpParameters == 0x1337C0DE)
	{
		// okay CreateThread test passed
		cout << endl << "TestThread: 0x" << hex << lpParameters << " ... pass" << endl
			 << endl
			 << "Now grab your favorite injector and inject stuff into me." << endl
			 << endl;
	}
	else
	{
		// oh no, something went seriously wrong
		cout << endl << "TestThread: 0x" << hex << lpParameters << " ... failed" << endl
			 << endl
			 << "Invalid parameter passed to TestThread" << endl
			 << endl;
	}
}

int main()
{
	cout << "BaseThreadStartThunk: 0x" << hex << BaseThreadStartThunk << endl;
	cout << "BaseThreadStart: 0x" << hex << BaseThreadStart << endl;
	
	// hook BaseThreadStart function
	PatchCode_Jmp(BaseThreadStartThunk_Jmp, (DWORD)__hook_BaseThreadStart);
	
	// make sure we dont block legitimate CreateThread call
	CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)TestThread, (LPVOID)0x1337C0DE, NULL, NULL);
	
	Sleep(INFINITE);
	return 0;
}
