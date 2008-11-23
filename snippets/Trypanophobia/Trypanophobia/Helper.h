#pragma once

// function prototyping
DWORD FindCode(const LPSTR pSig, const DWORD dwSize, const DWORD dwAddress, const DWORD dwLength);
DWORD GetTextSize(HMODULE hModule);
DWORD GetTextAddress(HMODULE hModule);

bool PatchCode_Jmp(DWORD dwAddress, DWORD dwHook);
