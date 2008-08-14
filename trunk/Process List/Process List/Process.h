#pragma once

#include <vector>
#include <string>

#include <windows.h>
#include <tlhelp32.h>

class Process
{
    // private fields
    unsigned int Id;
    std::wstring ProcessName;
    
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
     * get/set functions
     */
    unsigned int getId( void ) { return this->Id; }
    std::wstring getProcessName( void ) { return this->ProcessName; }
};
