#include <cstring>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

using std::memcmp;

DWORD FindCode(const LPSTR pSig, const DWORD dwSize, const DWORD dwAddress, const DWORD dwLength)
{
	// find the code specified by the signature
	for (DWORD i = dwAddress; i < (dwAddress + dwLength); ++i)
		if (memcmp((LPVOID)i, pSig, dwSize) == 0)
			return i;

	return NULL;
}

DWORD GetTextSize(HMODULE hModule)
{
	IMAGE_DOS_HEADER     *Dos    = (IMAGE_DOS_HEADER*)hModule;
	IMAGE_NT_HEADERS     *NT     = (IMAGE_NT_HEADERS*)((DWORD)hModule + Dos->e_lfanew);
	IMAGE_SECTION_HEADER *Header = (IMAGE_SECTION_HEADER*)((DWORD)NT + sizeof(IMAGE_NT_HEADERS));
	
	for (unsigned int i = 0; i < NT->FileHeader.NumberOfSections; ++i)
	{
		if (Header->Characteristics && IMAGE_SCN_CNT_CODE)
		{
			// text section
			return (Header->SizeOfRawData > Header->Misc.VirtualSize ? Header->Misc.VirtualSize : Header->SizeOfRawData);
		}

		++Header;
	}

	return NULL;
}

DWORD GetTextAddress(HMODULE hModule)
{
	IMAGE_DOS_HEADER     *Dos    = (IMAGE_DOS_HEADER*)hModule;
	IMAGE_NT_HEADERS     *NT     = (IMAGE_NT_HEADERS*)((DWORD)hModule + Dos->e_lfanew);
	IMAGE_SECTION_HEADER *Header = (IMAGE_SECTION_HEADER*)((DWORD)NT + sizeof(IMAGE_NT_HEADERS));
	
	for (unsigned int i = 0; i < NT->FileHeader.NumberOfSections; ++i)
	{
		if (Header->Characteristics && IMAGE_SCN_CNT_CODE)
		{
			// text section
			return ((DWORD)hModule + Header->VirtualAddress);
		}

		++Header;
	}

	return NULL;
}

bool PatchCode_Jmp(DWORD dwAddress, DWORD dwHook)
{
	DWORD dwOldProtect;
	
	// make the memory writable
	if (VirtualProtect((LPVOID)dwAddress, 5, PAGE_EXECUTE_READWRITE, &dwOldProtect))
	{
		// flush processor instruction cache
		FlushInstructionCache(GetCurrentProcess(), (LPCVOID)dwAddress, 5);
		
		// patch the address
		*(DWORD*)(dwAddress + 1) = (dwHook - dwAddress - 5);

		// restore the old memory protection
		VirtualProtect((LPVOID)dwAddress, 5, dwOldProtect, &dwOldProtect);
	}

	return false;
}
