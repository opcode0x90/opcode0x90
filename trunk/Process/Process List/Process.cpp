/*
 * Process.cpp - complete solution to your process needs
 */

#include "Process.h"

/*
 * constructor/destructor
 */
ProcessMemory::ProcessMemory( unsigned int Id )
{
    // initialize all the fields
    this->Id = Id;
}

/*
 * public functions
 */
std::string ProcessMemory::Read( unsigned int length )
{
    std::string buffer;
    HANDLE hProcess;
    
    // attempt to acquire a read access to the specified process
    hProcess = OpenProcess( PROCESS_VM_READ, FALSE, Id );
    
    if ( hProcess != NULL )
    {
        // allocate the buffer
        buffer = std::string( length, 0 );
        
        // read the fucking memory
        ReadProcessMemory( hProcess, (LPCVOID)this->address, (LPVOID)buffer.c_str(), length, NULL );
        
        // cleanup
        CloseHandle( hProcess );
    }
    
    // done
    return buffer;
}

std::wstring ProcessMemory::ReadUnicode( unsigned int length )
{
    std::wstring buffer;
    HANDLE hProcess;
    
    // attempt to acquire a read access to the specified process
    hProcess = OpenProcess( PROCESS_VM_READ, FALSE, Id );
    
    if ( hProcess != NULL )
    {
        // allocate the buffer
        buffer = std::wstring( length, 0 );
        
        // read the fucking memory
        ReadProcessMemory( hProcess, (LPCVOID)this->address, (LPVOID)buffer.c_str(), ( length * 2 ), NULL );
        
        // cleanup
        CloseHandle( hProcess );
    }
    
    // done
    return buffer;
}

int ProcessMemory::ReadInt( void )
{
    int buffer = 0;
    HANDLE hProcess;
    
    // attempt to acquire a read access to the specified process
    hProcess = OpenProcess( PROCESS_VM_READ, FALSE, Id );
    
    if ( hProcess != NULL )
    {
        // read the fucking memory
        ReadProcessMemory( hProcess, (LPCVOID)this->address, &buffer, 4, NULL );
        
        // cleanup
        CloseHandle( hProcess );
    }
    
    // done
    return buffer;
}

void ProcessMemory::Write( std::string data )
{
    HANDLE hProcess;
    
    // attempt to acquire a write access to the specified process
    hProcess = OpenProcess( PROCESS_VM_WRITE|PROCESS_VM_OPERATION , FALSE, Id );
    
    if ( hProcess != NULL )
    {
        // write the data into remote process
        WriteProcessMemory( hProcess, (LPVOID)this->address, (LPCVOID)data.c_str(), data.length(), NULL );
        
        // cleanup
        CloseHandle( hProcess );
    }
}

void ProcessMemory::WriteUnicode( std::wstring data )
{
    HANDLE hProcess;
    
    // attempt to acquire a write access to the specified process
    hProcess = OpenProcess( PROCESS_VM_WRITE|PROCESS_VM_OPERATION , FALSE, Id );
    
    if ( hProcess != NULL )
    {
        // write the data into remote process
        WriteProcessMemory( hProcess, (LPVOID)this->address, (LPCVOID)data.c_str(), ( data.length() * 2 ), NULL );
        
        // cleanup
        CloseHandle( hProcess );
    }
}

void ProcessMemory::WriteInt( int data )
{
    HANDLE hProcess;
    
    // attempt to acquire a write access to the specified process
    hProcess = OpenProcess( PROCESS_VM_WRITE|PROCESS_VM_OPERATION , FALSE, Id );
    
    if ( hProcess != NULL )
    {
        // write the data into remote process
        WriteProcessMemory( hProcess, (LPVOID)this->address, &data, 4, NULL );
        
        // cleanup
        CloseHandle( hProcess );
    }
}

/******************************************************************/

/*
 * constructor/destructor
 */
Process::Process( unsigned int Id, std::wstring ProcessName )
{
    // initialize all the fields
    this->Id = Id;
    this->ProcessName = ProcessName;
    this->Memory = ProcessMemory( Id );
}

/*
 * static functions
 */
std::vector<Process> Process::GetProcesses( void )
{
    std::vector<Process> v;
    
    HANDLE hSnapshot;
    PROCESSENTRY32 pe = {0};
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
    
    // get all processes
    v = Process::GetProcesses();
    
    // filter out unrelated process
    for ( unsigned int i = v.size(); i > 0; i-- )
    {
        // is this what we are looking for ?
        if ( v[ i - 1 ].getProcessName() != processName )
        {
            // none of your business
            v.erase( v.begin() + i - 1 );
        }
    }
    
    return v;
}
