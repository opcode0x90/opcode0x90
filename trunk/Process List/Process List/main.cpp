#include <iostream>

#include "Process.h"

using namespace std;

int main()
{
    vector<Process> v = Process::GetProcesses();
    
    for ( unsigned int i = 0; i < v.size(); i++ )
    {
        wcout << v[i].getProcessName() << L": " << v[i].getId() << endl;
    }
    
    system("pause");
    return 0;
}
