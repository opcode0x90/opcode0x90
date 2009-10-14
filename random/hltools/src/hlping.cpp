/*
 * hlping - part of hltools project
 *
 * A simple ping utility that sends A2A_PING to specified
 * hlds_client server
 *     by: opcode0x90
 *
 * Changelog:
 *     0.0.1a - Initial version
 */

#include <iostream>
#include <string>
#include <exception>
#include <boost/program_options.hpp>
#include <boost/asio.hpp>

#include "hlds_client.h"

/***********************************************************/

int main(int argc, char* argv[])
{
	using std::cout;
	using std::endl;
	namespace po = boost::program_options;

	std::string hostname, port;
	unsigned int count;
	float interval, timeout;

	po::options_description desc("Available Options");
	po::positional_options_description pod;

	// Declare the supported options.
	desc.add_options()
		("help", "print this help message")
		("port,p", po::value<std::string>(&port)->default_value("27015"), "hlds port")
		("hostname,h", po::value<std::string>(&hostname), "destination hostname")
		("count,c", po::value<unsigned int>(&count), "ping count")
		("interval,i", po::value<float>(&interval)->default_value(1.0), "ping interval")
		("timeout,t", po::value<float>(&timeout)->default_value(3.0),"timeout interval");
	pod.add("hostname", -1);

	po::variables_map vm;
	po::store(po::command_line_parser(argc, argv).options(desc).positional(pod).run(), vm);
	po::notify(vm);

	if (!vm.count("hostname") || vm.count("help"))
	{
		// display the help
		cout << "Usage: hlping [options] hostname" << endl
			<< endl
			<< desc << endl;
		return 1;
	}
	else if (timeout == 0.0)
	{
		// invalid timeout interval
		cout << "Invalid timeout interval" << endl
			<< endl
			<< "Usage: hlping [options] hostname" << endl
			<< endl
			<< desc << endl;
		return 1;
	}

	// construct the hlds client
	boost::asio::io_service io_service;
	hlds_client hlds(io_service, hostname, port);

	// begin the probe
	cout << endl
		<< "Pinging " << hostname << " [" << hlds.getReceiverEndpoint().address() << "] with A2A_PING: " << endl;

	for (unsigned int i = 1; count == 0 || i <= count; ++i)
	{
		try
		{
			// ping the specified hlds
			hlds_client::tick_type latency = hlds.ping(timeout);

			// hlds responded
			cout << "Reply from " << hlds.getSenderEndpoint().address() << ": seq=" << i << " time=" << latency << "ms" << endl;

			// wait for a while
			boost::asio::deadline_timer t(io_service, boost::posix_time::millisec(interval*1000));
			t.wait();
		}
		catch(std::exception& e)
		{
			std::cerr << e.what() << std::endl;
		}
	}
	return 0;
}
