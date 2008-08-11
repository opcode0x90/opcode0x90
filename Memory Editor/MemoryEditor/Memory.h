#pragma once

#include <string>
#include "windows.h"

class Memory
{
    /*
     * private fields
     */
    int hProcess;
    int hProcessID;
    
    void Dispose();
    
public:
    /*
     *
     */
    std::string *Content;
    
    /*
     * constructor/destructor
     */
    Memory( int processId );
    Memory( int handle, int processId );
    ~Memory();
    
    /*
     * public functions
     */
    bool Read( unsigned int address, unsigned int length );
    bool Write( unsigned int address );
    bool Write( unsigned int address, std::string content );
};
