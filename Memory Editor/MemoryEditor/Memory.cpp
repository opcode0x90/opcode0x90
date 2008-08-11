/*
 * memory.cpp - contains all the memory editing functions
 */

#include "Memory.h"

/*
 * constructor/destructor
 */
Memory::Memory( int processId )
{
    // initialize the fields
    this->Content = new std::string("");
    this->hProcessID = processId;
    
    // acquire and cache the handle to the specified process
    this->hProcess = (int)OpenProcess( PROCESS_VM_READ|PROCESS_VM_WRITE|PROCESS_VM_OPERATION, FALSE, processId );
}

Memory::Memory( int handle, int processId )
{
    // initialize the fields
    this->Content = new std::string("");
    this->hProcess = handle;
    this->hProcessID = processId;
}

Memory::~Memory()
{
    // cleanup
    delete this->Content;
    
    // close any opened handle
    CloseHandle( (HANDLE)this->hProcess );
}

/*
 * public functions
 */
bool Memory::Read( unsigned int address, unsigned int length )
{
    // allocate enough buffer to hold the memory content
    std::string *buffer = new std::string( length, NULL );
    
    // read the memory of remote process
    if ( ReadProcessMemory( (HANDLE)this->hProcess, (LPCVOID)address, (LPVOID)buffer->c_str(), length, NULL ) != 0 )
    {
        // replace the memory buffer with new content
        delete this->Content;
        this->Content = buffer;
        
        // done
        return true;
    }
    else
    {
        // cleanup
        delete buffer;
        
        // something's wrong
        return false;
    }
}

bool Memory::Write( unsigned int address )
{
    // just a wrapper for lazy coders
    return Memory::Write( address, *this->Content );
}

bool Memory::Write( unsigned int address, std::string content )
{
    // write the content to the remote address
    return ( WriteProcessMemory( (HANDLE)this->hProcess, (LPVOID)address, content.c_str(), content.size(), NULL) != 0 ) ? true : false;
}
