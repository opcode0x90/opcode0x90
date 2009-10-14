/*
 * hlds_client.cpp - part of hltools project
 *
 * Abstraction of common hlds_client functionalities
 * for all your needs
 *     by: opcode0x90
 *
 * Changelog:
 *     0.0.1a - Initial version
 */

#include <exception>
#include <boost/date_time/posix_time/posix_time.hpp>
#include <boost/bind.hpp>
#include <boost/range.hpp>

#include "hlds_client.h"

/***********************************************************/

//
// constructor
//
hlds_client::hlds_client(boost::asio::io_service& io_service, const std::string& hostname, const std::string& port /* = */ )
	: hostname(hostname),
	  port(port),
	  socket(io_service),
	  timeout_timer(io_service)
{
	using boost::asio::ip::udp;

	// resolve hostname into address
	udp::resolver        resolver(socket.get_io_service());
	udp::resolver::query query(udp::v4(), hostname, port);

	this->receiver_endpoint = *resolver.resolve(query);
}

/***********************************************************/

//
// functions
//
hlds_client::tick_type hlds_client::ping(const float& timeout /* = 3.0 */)
{
	using boost::posix_time::ptime;
	using boost::posix_time::time_duration;
	using boost::posix_time::microsec_clock;

	bool timedout = false;
	bool received = false;

	boost::array<char, 19> recv_buffer;

	if (!socket.is_open())
	{
		// initialize the socket
		socket.open(boost::asio::ip::udp::v4());
	}

	// reset everything
	socket.get_io_service().reset();

	// begin the timer
	ptime begin_time = microsec_clock::local_time();

	// ping the specified hlds
	socket.send_to(boost::asio::buffer(A2A_PING), receiver_endpoint);

	// prepare the timeout timer
	timeout_timer.expires_from_now(boost::posix_time::millisec(timeout*1000)); 
	timeout_timer.async_wait(
		boost::bind(
			&hlds_client::__ping_handle_timeout, this,
				_1,
				boost::ref(timedout))); 
	
	// now wait for hlds to reply
	socket.async_receive_from(
		boost::asio::buffer(recv_buffer), sender_endpoint,
		boost::bind(&hlds_client::__ping_handle_receive, this,
			boost::asio::placeholders::error,
			boost::asio::placeholders::bytes_transferred));

	// steady ...
	socket.get_io_service().run();

	// did we timed out?
	if (timedout)
	{
		// request timed out
		throw std::exception("Request timed out");
	}
	// is this a valid reply ?
	else if (A2A_PING_REPLY !=
		boost::make_iterator_range(
			recv_buffer.begin(),
			recv_buffer.begin() + A2A_PING_REPLY.size()))
	{
		// invalid or corrupted reply
		throw std::exception("Invalid or corrupted reply");
	}
	else
	{
		// calculate and return the latency
		ptime end_time = microsec_clock::local_time();
		time_duration duration = (end_time - begin_time);

		return duration.total_milliseconds();
	}
}

/***********************************************************/

//
// internal functions
//
void hlds_client::__ping_handle_receive(const boost::system::error_code& error, std::size_t bytes_transferred)
{
	if (error != boost::asio::error::operation_aborted)
	{
		// cancel the timeout timer
		timeout_timer.cancel();
	}
}

void hlds_client::__ping_handle_timeout(const boost::system::error_code& error, bool& timedout)
{
	if (error != boost::asio::error::operation_aborted)
	{
		// cancel all async operation
		socket.close();
		timedout = true;
	}
}

/***********************************************************/

//
// predefined packets
//
const boost::array<char, 5> hlds_client::A2A_PING       = { 'ÿ', 'ÿ', 'ÿ', 'ÿ', 'i' };
const boost::array<char, 5> hlds_client::A2A_PING_REPLY = { 'ÿ', 'ÿ', 'ÿ', 'ÿ', 'j' };

/***********************************************************/
