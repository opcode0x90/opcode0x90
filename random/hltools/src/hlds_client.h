/*
 * hlds_client.h - part of hltools project
 *
 * Header file for hlds_client.cpp
 *     by: opcode0x90
 *
 * Changelog:
 *     0.0.1a - Initial version
 */

#ifndef __HLDS_H__
#define __HLDS_H__

/***********************************************************/

#include <string>
#include <boost/asio.hpp>
#include <boost/array.hpp>

/***********************************************************/

class hlds_client
{
public:
	//
	// type aliases
	//
	typedef boost::posix_time::time_duration::tick_type tick_type;
	typedef boost::asio::ip::udp::endpoint endpoint;

	//
	// constructor
	//
	hlds_client(boost::asio::io_service& io_service, const std::string& hostname, const std::string& port = "27015");

	//
	// functions
	//
	tick_type ping(const float& timeout = 3.0);

	//
	// properties
	//
	inline const endpoint& getSenderEndpoint() const { return this->sender_endpoint; }
	inline const endpoint& getReceiverEndpoint() const { return this->receiver_endpoint; }

private:
	//
	// fields
	//
	std::string hostname, port;

	//
	// boost::asio objects
	//
	boost::asio::ip::udp::socket   socket;
	boost::asio::ip::udp::endpoint sender_endpoint;
	boost::asio::ip::udp::endpoint receiver_endpoint;

	boost::asio::deadline_timer    timeout_timer;

	//
	// predefined packets
	//
	static const boost::array<char, 5> A2A_PING;
	static const boost::array<char, 5> A2A_PING_REPLY;

	//
	// internal functions
	//
	void __ping_handle_receive(const boost::system::error_code& error, std::size_t bytes_transferred);
	void __ping_handle_timeout(const boost::system::error_code& error, bool& timedout);
};

/***********************************************************/

#endif /* __HLDS_H__ */
