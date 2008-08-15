#pragma once

#include <vector>
#include <string>

#include <windows.h>
#include <tlhelp32.h>

class ProcessMemory
{
    // private fields
    unsigned int Id;
    unsigned int address;
    
public:
    /*
     * constructor/destructor
     */
    ProcessMemory( void ) {}
    ProcessMemory( unsigned int Id );
    
    /*
     * public functions
     */
    std::string Read( unsigned int length );
    std::wstring ReadUnicode( unsigned int length );
    void Write( std::string data );
    void WriteUnicode( std::wstring data );
    
    /*
     * operator overloading
     */
    ProcessMemory& operator []( unsigned int address );
    
    /*
     * get/set functions
     */
    unsigned int getId( void ) { return this->Id; }
};

class Process
{
    // private fields
    unsigned int Id;
    std::wstring ProcessName;
    ProcessMemory Memory;
    
public:
    /*
     * constructor/destructor
     */
    Process( unsigned int Id, std::wstring ProcessName );
    
    /*
     * static functions
     */
    static std::vector<Process> GetProcesses( void );
    static std::vector<Process> GetProcessesByName( std::wstring processName );
    
    /*
     * operator overloading
     */
    ProcessMemory& operator []( unsigned int address );
    
    /*
     * get/set functions
     */
    unsigned int getId( void ) { return this->Id; }
    std::wstring& getProcessName( void ) { return this->ProcessName; }
    ProcessMemory& getMemory( void ) { return this->Memory; }
};
