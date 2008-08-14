/*
 * Process.cpp - complete solution to your process needs
 */

#include "Process.h"

/*
 * constructor/destructor
 */
Process::Process( unsigned int Id, std::wstring ProcessName )
{
    // initialize all the fields
    this->Id = Id;
    this->ProcessName = ProcessName;
}

/*
 * static functions
 */
std::vector<Process> Process::GetProcesses( void )
{
    std::vector<Process> v;
    
    HANDLE hSnapshot;
    PROCESSENTRY32W pe = {0};
    BOOL lRet;
    
    // attempt to acquire a snapshot of processes
    hSnapshot = CreateToolhelp32Snapshot( TH32CS_SNAPPROCESS, NULL );
    
    if ( hSnapshot != INVALID_HANDLE_VALUE )
    {
        // fill in the size
        pe.dwSize = sizeof( pe );
        
        // get the first process
        lRet = Process32First( hSnapshot, &pe );
        
        while ( lRet )
        {
            // add this process into our vector
            v.push_back( Process( pe.th32ProcessID, pe.szExeFile ) );
            
            // get the next process
            lRet = Process32Next( hSnapshot, &pe );
        }
        
        // cleanup
        CloseHandle( hSnapshot );
    }
    
    return v;
}

std::vector<Process> Process::GetProcessesByName( std::wstring processName )
{
    std::vector<Process> v;
    
    HANDLE hSnapshot;
    PROCESSENTRY32W pe = {0};
    BOOL lRet;
    
    // attempt to acquire a snapshot of processes
    hSnapshot = CreateToolhelp32Snapshot( TH32CS_SNAPPROCESS, NULL );
    
    if ( hSnapshot != INVALID_HANDLE_VALUE )
    {
        // fill in the size
        pe.dwSize = sizeof( pe );
        
        // get the first process
        lRet = Process32First( hSnapshot, &pe );
        
        while ( lRet )
        {
            // is this what we are looking for ?
            if ( processName.compare( pe.szExeFile ) == 0 )
            {
                // add this process into our vector
                v.push_back( Process( pe.th32ProcessID, processName ) );
            }
            
            // get the next process
            lRet = Process32Next( hSnapshot, &pe );
        }
        
        // cleanup
        CloseHandle( hSnapshot );
    }
    
    return v;
}
