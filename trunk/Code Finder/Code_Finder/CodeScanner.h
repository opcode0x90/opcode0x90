#pragma once

#include <string>
#include <windows.h>

class CodeScanner
{
public:
    /*
     * public functions
     */
    static unsigned int Scan( void *buffer, unsigned int len, std::string sig );
    static unsigned int Scan( void *buffer, unsigned int len, char* sig, unsigned int siglen );
};
