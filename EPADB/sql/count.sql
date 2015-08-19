select d.kks_id_signal,a.short_name,count(d.time) 
	from data_16_2015_07_14_15_51_event as d left join asu_signals_full as a on d.kks_id_signal=a.kks_id_signal 
	group by d.kks_id_signal,a.short_name 
	order by count(d.time) desc